using System;
using Cysharp.Threading.Tasks;
using UnityEngine;

namespace Unit.pureC.Unit
{
    public class Brute: IUnitAction
    {
        private const float PlayerDamageRate = 1.0f;
        private const float SpecificRate = 0.3f;
        private const int BuffDurationTurns = 3;
        private const float QTETimeScale = 0.001f;
        
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

        public async UniTask BeforeAttack()
        {
            await MessagePresenter.Instance.AppearMessage("ブルートの攻撃");
        }

        public UniTask Dead()
        {
            return UniTask.CompletedTask;
        }

        /// <inheritdoc/>
        public UniTask<EnemyMoveKinds> Act(int selfHeight, int selfWidth)
        {
            return UniTask.FromResult(EnemyMoveKinds.Attack);
        }

        public UniTask Specific(int selfHeight, int selfWidth)
        {
            return UniTask.CompletedTask;
        }

        public UniTask BeforeSpecific()
        {
            return UniTask.CompletedTask;
        }

        public UniTask OnTurnStart()
        {
            return UniTask.CompletedTask;
        }

        public UniTask OnTurnEnd()
        {
            return UniTask.CompletedTask;
        }

        public UniTask Damage()
        {
            return UniTask.CompletedTask;
        }
    }
}