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
        private const float SpecificRate = 0.8f;
        private const float QTETimeScale = 0.001f;
        
        public async UniTask BeforeAttack()
        {
            await MessageManager.Instance.AppearMessage("ビルダーの攻撃");
        }
        
        /// <inheritdoc/>
        public async UniTask Attack()
        {
            try
            {
                Time.timeScale = QTETimeScale;
                var damageValue = await BattleManager.PlayerDamage(PlayerDamageRate);
                Time.timeScale = 1.0f;

                UIPresenter.Instance.AppearDamageText($"{damageValue.damage}", PlayerController.Instance.transform.position).Forget();

                await UniTask.Delay(TimeSpan.FromSeconds(1.0f));
            }
            finally
            {
                BattleManager.ResetQTE();
                await CameraManager.Instance.ActResetCameraTarget();
            }
        }
        
        /// <inheritdoc/>
        public UniTask<EnemyMoveKinds> Act(int selfHeight, int selfWidth)
        {
            if (UnityEngine.Random.value < SpecificRate)
            {
                return UniTask.FromResult(EnemyMoveKinds.Specific);
            }

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
            await MessageManager.Instance.AppearMessage("ビルダーは壁を建設し始めた");
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
        public UniTask Damage()
        {
            return UniTask.CompletedTask;
        }
    }
}
