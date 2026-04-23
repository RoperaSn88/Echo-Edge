using Cysharp.Threading.Tasks;
using System;
using UnityEngine;

namespace Unit.pureC.Unit
{
    public class Skya: IUnitAction
    {
        private const float PlayerDamageRate = 1.0f;
        private const float SpecificRate = 0.2f;
        
        /// <inheritdoc/>
        public async UniTask Attack()
        {
            Time.timeScale = 0.001f;
            var damageValue = await BattleManager.PlayerDamage(PlayerDamageRate);
            Time.timeScale = 1.0f;

            UIPresenter.Instance.AppearDamageText($"{damageValue.damage}", PlayerController.Instance.transform.position).Forget();

            await UniTask.Delay(TimeSpan.FromSeconds(1.0f));

            BattleManager.ResetQTE();
            await CameraManager.Instance.ActResetCameraTarget();
        }
        
        /// <inheritdoc/>
        public async UniTask Act(int selfHeight, int selfWidth)
        {
            if (UnityEngine.Random.value < SpecificRate)
            {
                await Specific(selfHeight, selfWidth);
                return;
            }

            await Attack();
        }

        /// <inheritdoc/>
        public async UniTask Dead()
        {
            throw new System.NotImplementedException();
        }
        
        /// <inheritdoc/>
        public async UniTask Specific(int selfHeight, int selfWidth)
        {
            return;
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
