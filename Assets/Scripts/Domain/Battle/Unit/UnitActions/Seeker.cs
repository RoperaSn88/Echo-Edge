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
    /// 毎エネミーフェーズ、盤面上の『エナー』の数に応じて、召喚かバフのどちらか一方を行う。
    /// - エナーが2体未満なら、エナーを1〜3体召喚する
    /// - エナーが2体以上なら、(5 - エナー数) / 5 の確率で召喚し、それ以外はバフを行う
    /// バフは盤面上の『エナー』全員の攻撃力・防御力・移動量のいずれか1つ（等確率）を上昇させる（効果は重ねがけされる）。
    /// 召喚するエナーのレベルは、エナーを2体召喚するごとに+1される。
    /// 召喚では、スキルアニメーションのイベントで召喚先のマスへフラスコを投げ、着地したマスにエナーが現れる。
    /// </summary>
    public class Seeker : IUnitAction
    {
        /// <summary>
        /// 1回に召喚する『エナー』の最小数・最大数
        /// </summary>
        private const int MinSummonCount = 1;
        private const int MaxSummonCount = 3;

        /// <summary>
        /// 盤面上のエナーがこの数未満なら、必ず召喚する
        /// </summary>
        private const int AlwaysSummonBelow = 2;

        /// <summary>
        /// エナーが2体以上いるときの召喚確率の分母。召喚確率は (SummonRateBase - エナー数) / SummonRateBase
        /// </summary>
        private const int SummonRateBase = 5;

        /// <summary>
        /// この数だけエナーを召喚するごとに、召喚するレベルを+1する
        /// </summary>
        private const int SummonsPerLevelUp = 2;

        /// <summary>
        /// 強化1回あたりの上昇量。重ねがけできるよう、期限つきのバフではなくステータスを直接上げる
        /// </summary>
        private const int AttackBonus = 5;
        private const int DefendBonus = 3;
        private const int MoveBonus = 1;

        /// <summary>
        /// 最初に召喚する『エナー』のレベル（補正なし）
        /// </summary>
        private const int BaseSummonLevel = 0;

        /// <summary>
        /// バフで抽選される強化一覧（等確率で1つだけ抽選）
        /// </summary>
        private static readonly (Action<BattleStatus> apply, string label)[] EnhanceOptions =
        {
            (status => status.ChangeAttack(AttackBonus), $"攻撃力+{AttackBonus}"),
            (status => status.ChangeDefend(DefendBonus), $"防御力+{DefendBonus}"),
            (status => status.ChangeMove(MoveBonus), $"移動量+{MoveBonus}"),
        };

        /// <summary>
        /// このシーカーがこれまでに召喚した『エナー』の数。召喚レベルの計算に使う
        /// </summary>
        private int _summonedCount;

        /// <summary>
        /// このフェーズに召喚するか（false ならバフ）。<see cref="Act"/> で決める
        /// </summary>
        private bool _willSummon;

        /// <summary>
        /// このフェーズの召喚先のマス。<see cref="Act"/> で決め、フラスコの投げ先として予約する
        /// </summary>
        private List<(int h, int w)> _summonCells = new();

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
            // 毎フェーズ、召喚かバフのどちらかを行う。
            // フラスコはスキルアニメーションの途中で投げるため、召喚先はアニメーションより前のここで決めて予約しておく
            _summonCells.Clear();
            _willSummon = MapManager.Instance != null && ShouldSummon(CountEnarsOnBoard());
            if (_willSummon)
            {
                var summonCount = UnityEngine.Random.Range(MinSummonCount, MaxSummonCount + 1);
                _summonCells = FindSummonCells(selfHeight, selfWidth, summonCount);
            }

            // バフのときは空の予約にして、前の予約が残らないようにする
            if (FlaskThrower.Instance != null && UnitSpawner.Instance != null)
            {
                FlaskThrower.Instance.Reserve(_summonCells.Select(cell => UnitSpawner.Instance.GetCellWorldPosition(cell.h, cell.w)));
            }

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

            if (_willSummon)
            {
                await SummonEnars();
            }
            else
            {
                await EnhanceEnars();
            }

            await CameraManager.Instance.ActResetCameraTarget();
        }

        /// <summary>
        /// 召喚するかどうかを決める。
        /// エナーが2体未満なら必ず召喚し、2体以上なら (5 - エナー数) / 5 の確率で召喚する（それ以外はバフ）。
        /// </summary>
        private static bool ShouldSummon(int enarCount)
        {
            if (enarCount < AlwaysSummonBelow) return true;

            var summonRate = Mathf.Clamp01((SummonRateBase - enarCount) / (float)SummonRateBase);
            return UnityEngine.Random.value < summonRate;
        }

        private static int CountEnarsOnBoard()
        {
            return MapManager.Instance.GetUnitPositionsSnapshot()
                .Count(info => info.unit.GetEnemyKind() == EnemyKinds.Enar);
        }

        /// <summary>
        /// 召喚する『エナー』のレベル。エナーを2体召喚するごとに+1する
        /// </summary>
        private int CurrentSummonLevel => BaseSummonLevel + _summonedCount / SummonsPerLevelUp;

        /// <summary>
        /// <see cref="Act"/> で決めたマスへ投げたフラスコの着地を待ち、そこに『エナー』を召喚する
        /// </summary>
        private async UniTask SummonEnars()
        {
            var cells = _summonCells;
            if (cells.Count == 0)
            {
                await MessagePresenter.Instance.AppearMessage("エナーを呼び出す場所がない");
                await UniTask.Delay(TimeSpan.FromSeconds(1f));
                return;
            }

            // スキルアニメーションでまだ投げていなければここで投げ、全部の着地を待つ
            if (FlaskThrower.Instance != null)
            {
                await FlaskThrower.Instance.WaitLandedAsync(UnitSpawner.Instance.SeekerStandPoint);
            }

            // 召喚先は Act でまとめて決めてある（同じマスへの重複登録を防ぐ）
            var size = await EnemyStatusLoader.TryLoadSize((int)EnemyKinds.Enar);
            var summoned = 0;
            foreach (var (h, w) in cells)
            {
                // 予約してから着地までの間にふさがったマスには出さない
                if (MapManager.Instance.GetUnitAt(h, w) != null) continue;

                var unit = UnitFactory.Create(EnemyKinds.Enar, h, w, size);
                await unit.LoadStatus(EnemyKinds.Enar, CurrentSummonLevel);
                UnitSpawner.Instance.SpawnView(unit, EnemyKinds.Enar);
                _summonedCount++;

                // 全滅クリア条件の残り敵数を増やす
                await DomainEventDispatcher.Dispatch(new EnemySpawnedEvent(new UnitPosition(h, w)));
                summoned++;
            }

            await MessagePresenter.Instance.AppearMessage($"エナーが{summoned}体現れた！");
            await UniTask.Delay(TimeSpan.FromSeconds(1f));
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
