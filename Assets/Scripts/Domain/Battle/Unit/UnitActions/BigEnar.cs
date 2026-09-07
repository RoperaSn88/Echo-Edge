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
    /// 『でかエナー』の行動を定義するクラス。
    /// 『エナー』の見守り係で、2x2マスを占有する(EnemySize.Large)。
    /// 通常攻撃は隣接時(width=0)のみ行う。
    /// スキルは場所を問わず発動でき、自身のHPを消費することで発動し、
    /// ステージ上の『エナー』全員にランダムな効果を99ターン付与する。
    /// </summary>
    public class BigEnar : IUnitAction
    {
        private const float PlayerDamageRate = 1.0f;
        private const float SpecificRate = 0.3f;
        private const float QTETimeScale = 0.001f;

        /// <summary>
        /// スキル発動時に消費するHP
        /// </summary>
        private const int SkillHpCost = 20;

        /// <summary>
        /// スキルで付与する効果の持続ターン数
        /// </summary>
        private const int BuffDurationTurns = 99;

        /// <summary>
        /// スキル発動時に抽選される効果一覧。1つだけ抽選され、ステージ上の『エナー』全員に付与される。
        /// </summary>
        private static readonly (Func<IBuff> factory, string label)[] SkillBuffOptions =
        {
            (() => new AttackBuff(), "攻撃力+5"),
            (() => new MoveBuff(), "移動力+1"),
            (() => new DefendBuff(), "防御力+3"),
        };

        public async UniTask BeforeAttack()
        {
            await MessagePresenter.Instance.AppearMessage("でかエナーの攻撃");
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
            var selfStatus = MapManager.Instance?.GetUnitAt(selfHeight, selfWidth)?.GetStatus();

            // HPコストを支払える時のみスキルを選択肢に入れる（自滅を避けるための安全策）
            if (selfStatus != null && selfStatus.HP > SkillHpCost && UnityEngine.Random.value < SpecificRate)
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
            await MessagePresenter.Instance.AppearMessage("でかエナーは自らのエネルギーを解き放つ");
        }

        /// <inheritdoc/>
        public async UniTask Specific(int selfHeight, int selfWidth)
        {
            if (MapManager.Instance == null) return;

            var selfUnit = MapManager.Instance.GetUnitAt(selfHeight, selfWidth);
            var selfStatus = selfUnit?.GetStatus();
            if (selfUnit == null || selfStatus == null) return;

            // スキル発動コストとして自身のHPを消費する（防御力・無敵状態は考慮しない）
            await selfUnit.ConsumeHP(SkillHpCost);

            // 効果はスキル発動時に1つだけ抽選される
            var choice = SkillBuffOptions[UnityEngine.Random.Range(0, SkillBuffOptions.Length)];

            var enars = MapManager.Instance.GetUnitPositionsSnapshot()
                .Where(info => info.unit.GetEnemyKind() == EnemyKinds.Enar)
                .ToList();

            foreach (var info in enars)
            {
                info.unit.GetStatus()?.AddBuff(choice.factory(), BuffDurationTurns);
            }

            // 抽選結果をテキストへ反映する
            await MessagePresenter.Instance.AppearMessage($"エナー全員に「{choice.label}」を付与した！");

            await UniTask.Delay(TimeSpan.FromSeconds(1f));
            await CameraManager.Instance.ActResetCameraTarget();
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
