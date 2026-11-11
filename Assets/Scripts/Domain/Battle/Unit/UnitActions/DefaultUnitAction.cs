using System;
using Cysharp.Threading.Tasks;
using UnityEngine;

using EchoEdge.App.Battle;
using EchoEdge.Infra.Camera;
using EchoEdge.Presenter.Player;
using EchoEdge.Presenter.UI;

namespace EchoEdge.Domain.Battle
{
    public class DefaultUnitAction : IUnitAction
    {
        private const float PlayerDamageRate = 1.0f;
        private const string EnemyAttackMessage = "敵の攻撃";
        private const float SpecificRate = 0.3f;

        public async UniTask BeforeAttack()
        {
            await MessagePresenter.Instance.AppearMessage("敵の攻撃");
        }

        /// <inheritdoc/>
        public async UniTask Attack()
        {
            var messageManager = MessagePresenter.Instance;
            if (messageManager != null)
            {
                await messageManager.AppearMessage(EnemyAttackMessage);
            }

            try
            {
                await BattleManager.PlayerDamageWithText(PlayerDamageRate);

                await UniTask.Delay(TimeSpan.FromSeconds(1.0f));
            }
            finally
            {
                if (messageManager != null)
                {
                    messageManager.DisappearMessage().Forget();
                }
                await CameraManager.Instance.ActResetCameraTarget();
            }
        }

        /// <inheritdoc/>
        public async UniTask<EnemyMoveKinds> Act(int selfHeight, int selfWidth)
        {
            if (UnityEngine.Random.value < SpecificRate)
            {
                await Specific(selfHeight, selfWidth);
                return EnemyMoveKinds.Specific;
            }

            return EnemyMoveKinds.Attack;
        }

        /// <inheritdoc/>
        public async UniTask Dead()
        {
            // 死亡時と同時に発生する処理
        }

        public async UniTask BeforeSpecific()
        {
            await MessagePresenter.Instance.AppearMessage("特殊行動");
        }

        /// <inheritdoc/>
        public async UniTask Specific(int selfHeight, int selfWidth)
        {

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

        }
    }
}
