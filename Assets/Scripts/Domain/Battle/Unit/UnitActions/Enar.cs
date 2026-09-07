using Cysharp.Threading.Tasks;
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

using EchoEdge.App.Battle;
using EchoEdge.Infra.Camera;
using EchoEdge.Presenter.Player;
using EchoEdge.Presenter.UI;

namespace EchoEdge.Domain.Battle
{
    /// <summary>
    /// 『エナー』の行動を定義するクラス。
    /// 複数体で運用されることを想定した敵キャラで、通常攻撃は隣接時(width=0)のみ行う。
    /// スキルは場所を問わず発動でき、2マス以内に他の『エナー』がいる場合、
    /// そのうち1体を犠牲にして自身のステータスを強化＆回復する。
    /// 吸収は1ターンにつき1回まで（エナー全体で共有する制限）。
    /// </summary>
    public class Enar : IUnitAction
    {
        /// <summary>
        /// このターンに既に吸収が行われたか。
        /// エナーが複数体いても1ターンに1回しか吸収させないため、全個体で共有する。
        /// 各個体の OnTurnStart（敵フェイズの行動開始前に全ユニット分が実行される）でリセットする。
        /// </summary>
        private static bool _hasAbsorbedThisTurn;

        private const float PlayerDamageRate = 1.0f;
        private const float SpecificRate = 0.3f;
        private const float QTETimeScale = 0.001f;

        /// <summary>
        /// スキルの対象を探す範囲（マス数）
        /// </summary>
        private const int SkillRange = 3;

        /// <summary>
        /// スキル成功時の自身への強化量
        /// </summary>
        private const int SkillAttackBonus = 5;
        private const int SkillDefendBonus = 2;
        private const int SkillHealAmount = 20;

        public async UniTask BeforeAttack()
        {
            await MessagePresenter.Instance.AppearMessage("エナーの攻撃");
        }

        /// <inheritdoc/>
        public async UniTask Attack()
        {
            Time.timeScale = QTETimeScale;
            var damageValue = await BattleManager.PlayerDamage(PlayerDamageRate);
            Time.timeScale = 1.0f;

            UIPresenter.Instance.AppearDamageText($"{damageValue.damage}", PlayerController.Instance.transform.position).Forget();

            await UniTask.Delay(TimeSpan.FromSeconds(1.0f));

            BattleManager.ResetQTE();
            await CameraManager.Instance.ActResetCameraTarget();
        }

        /// <inheritdoc/>
        public UniTask<EnemyMoveKinds> Act(int selfHeight, int selfWidth)
        {
            // スキルはどこにいても発動できるが、犠牲にできる他の『エナー』が近くにいて、
            // かつこのターンにまだ吸収が行われていない時のみ選択肢に入る
            if (!_hasAbsorbedThisTurn && FindSacrificeCandidates(selfHeight, selfWidth).Count > 0) //  && UnityEngine.Random.value < SpecificRate
            {
                return UniTask.FromResult(EnemyMoveKinds.Specific);
            }

            // 通常攻撃は隣接時(width=0)のみ
            if (selfWidth == 0)
            {
                return UniTask.FromResult(EnemyMoveKinds.Attack);
            }

            return UniTask.FromResult(EnemyMoveKinds.None);
        }

        /// <inheritdoc/>
        public async UniTask Dead()
        {
            await UniTask.CompletedTask;
        }

        public async UniTask BeforeSpecific()
        {
            await MessagePresenter.Instance.AppearMessage("エナーは仲間を吸収にしようとしている");
        }

        /// <inheritdoc/>
        public async UniTask Specific(int selfHeight, int selfWidth)
        {
            if (MapManager.Instance == null) return;
            if (_hasAbsorbedThisTurn) return;

            var selfUnit = MapManager.Instance.GetUnitAt(selfHeight, selfWidth);
            var selfStatus = selfUnit?.GetStatus();
            if (selfUnit == null || selfStatus == null) return;

            var candidates = FindSacrificeCandidates(selfHeight, selfWidth);
            if (candidates.Count == 0) return;

            // 吸収の実行が確定した時点で、このターンの吸収枠を消費する
            _hasAbsorbedThisTurn = true;

            var sacrifice = candidates[UnityEngine.Random.Range(0, candidates.Count)];
            await KillSacrifice(sacrifice.unit);

            // 自身のステータスを強化＆回復する
            selfStatus.ChangeAttack(SkillAttackBonus);
            selfStatus.ChangeDefend(SkillDefendBonus);
            await selfUnit.Heal(SkillHealAmount);

            await UniTask.Delay(TimeSpan.FromSeconds(1f));
            await CameraManager.Instance.ActResetCameraTarget();
        }

        /// <summary>
        /// 自分以外の『エナー』のうち、SkillRangeマス以内にいるものを探す
        /// </summary>
        private static List<(IUnit unit, int h, int w)> FindSacrificeCandidates(int selfHeight, int selfWidth)
        {
            if (MapManager.Instance == null) return new List<(IUnit, int, int)>();

            return MapManager.Instance.GetUnitPositionsSnapshot()
                .Where(info => info.h != selfHeight || info.w != selfWidth)
                .Where(info => info.unit.GetEnemyKind() == EnemyKinds.Enar)
                .Where(info => Math.Abs(info.h - selfHeight) + Math.Abs(info.w - selfWidth) <= SkillRange)
                .ToList();
        }

        /// <summary>
        /// 対象の『エナー』を犠牲にして死亡させる。
        /// 防御力・無敵状態は考慮せず確実に死亡し、経験値・エナジーの撃破報酬は発生しない。
        /// 死亡演出・HPゲージ・マップからの除去は BaseUnit 側の犠牲処理が担当する。
        /// </summary>
        private static async UniTask KillSacrifice(IUnit target)
        {
            if (target?.GetStatus() == null) return;

            await target.Sacrifice();
        }

        /// <inheritdoc/>
        public UniTask OnTurnStart()
        {
            // 敵フェイズの行動開始前に全ユニットの OnTurnStart が実行されるため、
            // ここでリセットすればこのターンの吸収枠が必ず 1 回に戻る
            _hasAbsorbedThisTurn = false;
            return UniTask.CompletedTask;
        }

        /// <inheritdoc/>
        public UniTask OnTurnEnd()
        {
            return UniTask.CompletedTask;
        }

        /// <inheritdoc/>
        public async UniTask Damage()
        {
            throw new System.NotImplementedException();
        }
    }
}
