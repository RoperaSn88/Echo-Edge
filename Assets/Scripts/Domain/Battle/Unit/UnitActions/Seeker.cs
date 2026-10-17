using Cysharp.Threading.Tasks;
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

using EchoEdge.App.Battle;
using EchoEdge.Infra.Battle;
using EchoEdge.Infra.Camera;
using EchoEdge.Presenter.Battle;
using EchoEdge.Presenter.UI;

namespace EchoEdge.Domain.Battle
{
    /// <summary>
    /// 『シーカー』の行動を定義するクラス。
    /// ステージの奥地（盤面の外）に立つ中ボスで、攻撃も移動もしない。
    /// 毎エネミーフェーズ、『エナー』を1〜3体、自身の周辺のマスに召喚する。
    /// 召喚後、確率で盤面上の『エナー』全員の攻撃力・防御力・移動量のいずれか1つを上昇させる（効果は重ねがけされる）。
    /// </summary>
    public class Seeker : IUnitAction
    {
        /// <summary>
        /// 1回に召喚する『エナー』の最小数・最大数
        /// </summary>
        private const int MinSummonCount = 1;
        private const int MaxSummonCount = 3;

        /// <summary>
        /// 召喚後に強化を行う確率
        /// </summary>
        private const float EnhanceRate = 0.5f;

        /// <summary>
        /// 強化1回あたりの上昇量。重ねがけできるよう、期限つきのバフではなくステータスを直接上げる
        /// </summary>
        private const int AttackBonus = 5;
        private const int DefendBonus = 3;
        private const int MoveBonus = 1;

        /// <summary>
        /// 召喚した『エナー』に適用するレベル。召喚直後は弱く、シーカーの強化で育つ想定のため補正なし(0)
        /// </summary>
        private const int SummonLevel = 0;

        /// <summary>
        /// 召喚後に抽選される強化一覧（1つだけ抽選）
        /// </summary>
        private static readonly (Action<BattleStatus> apply, string label)[] EnhanceOptions =
        {
            (status => status.ChangeAttack(AttackBonus), $"攻撃力+{AttackBonus}"),
            (status => status.ChangeDefend(DefendBonus), $"防御力+{DefendBonus}"),
            (status => status.ChangeMove(MoveBonus), $"移動量+{MoveBonus}"),
        };

        /// <inheritdoc/>
        public UniTask Attack()
        {
            // 攻撃はしない
            return UniTask.CompletedTask;
        }

        /// <inheritdoc/>
        public UniTask BeforeAttack()
        {
            return UniTask.CompletedTask;
        }

        /// <inheritdoc/>
        public UniTask Dead()
        {
            return UniTask.CompletedTask;
        }

        /// <inheritdoc/>
        public UniTask<EnemyMoveKinds> Act(int selfHeight, int selfWidth)
        {
            // 毎フェーズ必ず召喚する
            return UniTask.FromResult(EnemyMoveKinds.Specific);
        }

        /// <inheritdoc/>
        public async UniTask BeforeSpecific()
        {
            await MessagePresenter.Instance.AppearMessage("シーカーは装置を開いた");
        }

        /// <inheritdoc/>
        public async UniTask Specific(int selfHeight, int selfWidth)
        {
            if (MapManager.Instance == null || UnitSpawner.Instance == null) return;

            var summonCount = UnityEngine.Random.Range(MinSummonCount, MaxSummonCount + 1);
            var cells = FindSummonCells(selfHeight, selfWidth, summonCount);
            if (cells.Count == 0)
            {
                await MessagePresenter.Instance.AppearMessage("エナーを呼び出す場所がない");
                await UniTask.Delay(TimeSpan.FromSeconds(1f));
                await CameraManager.Instance.ActResetCameraTarget();
                return;
            }

            // 召喚先は先にまとめて決めておく（同じマスへの重複登録を防ぐ）
            var size = await EnemyStatusLoader.TryLoadSize((int)EnemyKinds.Enar);
            foreach (var (h, w) in cells)
            {
                var unit = UnitFactory.Create(EnemyKinds.Enar, h, w, size);
                await unit.LoadStatus(EnemyKinds.Enar, SummonLevel);
                UnitSpawner.Instance.SpawnView(unit, EnemyKinds.Enar);

                // 全滅クリア条件の残り敵数を増やす
                await DomainEventDispatcher.Dispatch(new EnemySpawnedEvent(new UnitPosition(h, w)));
            }

            await MessagePresenter.Instance.AppearMessage($"エナーが{cells.Count}体現れた！");
            await UniTask.Delay(TimeSpan.FromSeconds(1f));

            if (UnityEngine.Random.value < EnhanceRate)
            {
                await EnhanceEnars();
            }

            await CameraManager.Instance.ActResetCameraTarget();
        }

        /// <summary>
        /// 盤面上の『エナー』全員に、抽選した強化を1つ与える
        /// </summary>
        private static async UniTask EnhanceEnars()
        {
            var choice = EnhanceOptions[UnityEngine.Random.Range(0, EnhanceOptions.Length)];

            var enars = MapManager.Instance.GetUnitPositionsSnapshot()
                .Where(info => info.unit.GetEnemyKind() == EnemyKinds.Enar)
                .ToList();

            foreach (var info in enars)
            {
                var status = info.unit.GetStatus();
                if (status != null) choice.apply(status);
            }

            await MessagePresenter.Instance.AppearMessage($"シーカーはエナー全員に「{choice.label}」を与えた！");
            await UniTask.Delay(TimeSpan.FromSeconds(1f));
        }

        /// <summary>
        /// 召喚先のマスを探す。
        /// シーカー自身は盤面外にいるため、CSV で指定された座標（基準マス）に近い空きマスから順に選ぶ。
        /// 同じ距離のマスはランダムな順で選ぶ。
        /// </summary>
        private static List<(int h, int w)> FindSummonCells(int baseHeight, int baseWidth, int count)
        {
            var map = MapManager.Instance;
            var candidates = new List<(int h, int w, int distance, float tieBreak)>();

            for (var h = 0; h < map.Height; h++)
            {
                for (var w = 0; w < map.Width; w++)
                {
                    if (map.GetUnitAt(h, w) != null) continue;

                    var distance = Math.Abs(h - baseHeight) + Math.Abs(w - baseWidth);
                    candidates.Add((h, w, distance, UnityEngine.Random.value));
                }
            }

            return candidates
                .OrderBy(c => c.distance)
                .ThenBy(c => c.tieBreak)
                .Take(count)
                .Select(c => (c.h, c.w))
                .ToList();
        }

        /// <inheritdoc/>
        public UniTask OnTurnStart()
        {
            return UniTask.CompletedTask;
        }

        /// <inheritdoc/>
        public UniTask OnTurnEnd()
        {
            return UniTask.CompletedTask;
        }

        /// <inheritdoc/>
        public UniTask Damage()
        {
            return UniTask.CompletedTask;
        }
    }
}
