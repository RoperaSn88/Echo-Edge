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
    /// </summary>
    public class Enar : IUnitAction
    {
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
            // スキルはどこにいても発動できるが、犠牲にできる他の『エナー』が近くにいる時のみ選択肢に入る
            if (FindSacrificeCandidates(selfHeight, selfWidth).Count > 0) //  && UnityEngine.Random.value < SpecificRate
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

            var selfStatus = MapManager.Instance.GetUnitAt(selfHeight, selfWidth)?.GetStatus();
            if (selfStatus == null) return;

            var candidates = FindSacrificeCandidates(selfHeight, selfWidth);
            if (candidates.Count == 0) return;

            var sacrifice = candidates[UnityEngine.Random.Range(0, candidates.Count)];
            await KillSacrifice(sacrifice.unit);

            // 自身のステータスを強化＆回復する
            selfStatus.ChangeAttack(SkillAttackBonus);
            selfStatus.ChangeDefend(SkillDefendBonus);
            selfStatus.Heal(SkillHealAmount);

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
        /// 対象の『エナー』を、防御力を考慮しても確実に致死量となるダメージで死亡させる
        /// </summary>
        private static async UniTask KillSacrifice(IUnit target)
        {
            var status = target.GetStatus();
            if (status == null) return;

            var lethalDamage = status.MaxHP + status.Defend;
            BattleManager.RegisterEnemy(status);
            
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
        public async UniTask Damage()
        {
            throw new System.NotImplementedException();
        }
    }
}
