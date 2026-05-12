using Cysharp.Threading.Tasks;
using System;
using UnityEngine;

namespace Unit.pureC.Unit
{
    public class Skya: IUnitAction, IFlyingUnit
    {
        private const float SpecificRate = 0.8f;
        private const float SpecificDamageRate = 3.5f;
        private const float QTETimeScale = 0.001f;

        /// <summary>
        /// 現在飛行中かどうか
        /// </summary>
        private bool _isFlying = false;

        /// <inheritdoc/>
        public bool IsFlying => _isFlying;

        public async UniTask BeforeAttack()
        {
            await MessageManager.Instance.AppearMessage("スカイアの攻撃");
        }
        
        public async UniTask WaitToFlyMessage()
        {
            await MessageManager.Instance.AppearMessage("スカイアは飛び始める");
        }

        public async UniTask WaitFlyingMessage()
        {
            await MessageManager.Instance.AppearMessage("スカイアはビームを放つ");
        }
        
        /// <inheritdoc/>
        public async UniTask Attack()
        {
            Time.timeScale = QTETimeScale;
            var damageValue = await BattleManager.PlayerDamage(1.0f);
            Time.timeScale = 1.0f;

            UIPresenter.Instance.AppearDamageText($"{damageValue.damage}", PlayerController.Instance.transform.position).Forget();

            await UniTask.Delay(TimeSpan.FromSeconds(1.0f));

            BattleManager.ResetQTE();
            await CameraManager.Instance.ActResetCameraTarget();
        }
        
        /// <inheritdoc/>
        public async UniTask<EnemyMoveKinds> Act(int selfHeight, int selfWidth)
        {
            // 飛行中は必ず特殊行動（ビーム攻撃）を行う
            if (_isFlying)
            {
                return EnemyMoveKinds.Specific;
            }

            if (UnityEngine.Random.value < SpecificRate)
            {
                return EnemyMoveKinds.Specific;
            }

            return EnemyMoveKinds.Attack;
        }

        /// <inheritdoc/>
        public async UniTask Dead()
        {
            await UniTask.CompletedTask;
        }
        
        public async UniTask BeforeSpecific()
        {
            await MessageManager.Instance.AppearMessage("スカイアは空を飛び始めた");
        }
        
        /// <inheritdoc/>
        public async UniTask Specific(int selfHeight, int selfWidth)
        {
            var status = MapManager.Instance?.GetUnitAt(selfHeight, selfWidth)?.GetStatus();

            if (!_isFlying)
            {
                // 1ターン目：飛び上がり、ダメージ無効を有効化
                _isFlying = true;
                if (status != null)
                {
                    status.IsInvincible = true;
                }
            }
            else
            {
                // 2ターン目：ビーム攻撃（2倍ダメージ）
                Time.timeScale = QTETimeScale;
                var damageValue = await BattleManager.PlayerDamage(SpecificDamageRate);
                Time.timeScale = 1.0f;

                UIPresenter.Instance.AppearDamageText($"{damageValue.damage}", PlayerController.Instance.transform.position).Forget();

                await UniTask.Delay(TimeSpan.FromSeconds(1.0f));

                BattleManager.ResetQTE();

                // 地上に戻り、ダメージ無効を解除
                _isFlying = false;
                if (status != null)
                {
                    status.IsInvincible = false;
                }
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
        public async UniTask Damage()
        {
            throw new System.NotImplementedException();
        }
    }
}
