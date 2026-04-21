using Cysharp.Threading.Tasks;
using System;
using UnityEngine;

namespace Unit.pureC.Unit
{
    public class Builder: IUnitAction
    {
        private const float PlayerDamageRate = 1.0f;
        private const float MessageTime = 0.6f;
        private const string BuilderAttackMessage = "ビルダーの攻撃";
        
        /// <inheritdoc/>
        public async UniTask Attack()
        {
            if (MessageManager.Instance != null)
            {
                await MessageManager.Instance.AppearMessage(BuilderAttackMessage);
                await UniTask.Delay(TimeSpan.FromSeconds(MessageTime));
                await MessageManager.Instance.DisappearMessage();
            }

            Time.timeScale = 0.001f;
            var damageValue = await BattleManager.PlayerDamage(PlayerDamageRate);
            Time.timeScale = 1.0f;

            UIPresenter.Instance.AppearDamageText($"{damageValue.damage}", PlayerController.Instance.transform.position).Forget();

            await UniTask.Delay(TimeSpan.FromSeconds(1.0f));

            BattleManager.ResetQTE();
            await CameraManager.Instance.ActResetCameraTarget();
        }
        
        /// <inheritdoc/>
        public async UniTask Dead()
        {
            throw new System.NotImplementedException();
        }

        /// <inheritdoc/>
        public async UniTask Specific()
        {
            throw new System.NotImplementedException();
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
