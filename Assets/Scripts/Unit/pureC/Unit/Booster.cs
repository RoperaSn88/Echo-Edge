using Cysharp.Threading.Tasks;
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace Unit.pureC.Unit
{
    public class Booster: IUnitAction
    {
        private const float PlayerDamageRate = 1.0f;
        private const float SpecificRate = 0.2f;
        private const int BuffDurationTurns = 3;

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
            if (MapManager.Instance == null) return;

            List<(IUnit unit, int h, int w)> allUnits = MapManager.Instance.GetUnitPositionsSnapshot();

            // 他の敵がバフをかかっていないならばSpecificを発動する
            bool anyOtherBuffed = allUnits
                .Where(u => u.h != selfHeight || u.w != selfWidth)
                .Any(u => u.unit.GetStatus().HasBuff(BuffKinds.Move));

            if (anyOtherBuffed) return;

            // 全ての敵のBattleStatusのmoveを1上昇させる
            foreach (var unitInfo in allUnits)
            {
                unitInfo.unit.GetStatus().AddBuff(new MoveBuff(), BuffDurationTurns);
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
