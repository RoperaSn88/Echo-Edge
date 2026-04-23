using Cysharp.Threading.Tasks;
using System.Collections.Generic;
using System.Linq;
using System;
using UnityEngine;

namespace Unit.pureC.Unit
{
    public class Builder: IUnitAction
    {
        private const float PlayerDamageRate = 1.0f;
        private const string BuilderAttackMessage = "ビルダーの攻撃";
        private const float SpecificRate = 0.6f;
        
        /// <inheritdoc/>
        public async UniTask Attack()
        {
            var messageManager = MessageManager.Instance;
            if (messageManager != null)
            {
                await messageManager.AppearMessage(BuilderAttackMessage);
            }

            try
            {
                Time.timeScale = 0.001f;
                var damageValue = await BattleManager.PlayerDamage(PlayerDamageRate);
                Time.timeScale = 1.0f;

                UIPresenter.Instance.AppearDamageText($"{damageValue.damage}", PlayerController.Instance.transform.position).Forget();

                await UniTask.Delay(TimeSpan.FromSeconds(1.0f));
            }
            finally
            {
                if (messageManager != null)
                {
                    messageManager.DisappearMessage().Forget();
                }

                BattleManager.ResetQTE();
                await CameraManager.Instance.ActResetCameraTarget();
            }
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
            if (MapManager.Instance == null || BuildingManager.Instance == null)
            {
                return;
            }

            List<(IUnit unit, int h, int w)> targetUnits = MapManager.Instance.GetUnitPositionsSnapshot()
                .Where(unitInfo => unitInfo.unit.CanMove() && (unitInfo.h != selfHeight || unitInfo.w != selfWidth))
                .ToList();

            if (targetUnits.Count == 0)
            {
                return;
            }

            var target = targetUnits[UnityEngine.Random.Range(0, targetUnits.Count)];
            int wallWidth = target.w - 1;

            for (int hOffset = -1; hOffset <= 1; hOffset++)
            {
                int wallHeight = target.h + hOffset;
                if (MapManager.Instance.GetUnitAt(wallHeight, wallWidth) != null)
                {
                    continue;
                }
                BuildingManager.Instance.TrySetBuilderWall(wallHeight, wallWidth);
            }
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
