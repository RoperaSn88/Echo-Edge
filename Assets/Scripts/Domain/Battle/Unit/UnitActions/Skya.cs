using Cysharp.Threading.Tasks;
using System;
using UnityEngine;

using EchoEdge.App.Battle;
using EchoEdge.Infra.Camera;
using EchoEdge.Presenter.Player;
using EchoEdge.Presenter.UI;

namespace EchoEdge.Domain.Battle
{
    public class Skya: IUnitAction, IFlyingUnit
    {
        private const float SpecificRate = 0.3f;
        private const float SpecificDamageRate = 2.5f;

        /// <summary>
        /// 現在飛行中かどうか
        /// </summary>
        private bool _isFlying = false;

        /// <inheritdoc/>
        public bool IsFlying => _isFlying;

        public async UniTask BeforeAttack()
        {
            await MessagePresenter.Instance.AppearMessage("スカイアの攻撃");
        }
        
        public async UniTask WaitToFlyMessage()
        {
            await MessagePresenter.Instance.AppearMessage("スカイアは飛び始める");
        }

        public async UniTask WaitFlyingMessage()
        {
            await MessagePresenter.Instance.AppearMessage("スカイアはビームを放つ");
        }

        /// <inheritdoc/>
        public void CancelFlying()
        {
            // 溜めていたビームは破棄して地上に戻る
            _isFlying = false;
        }
        
        /// <inheritdoc/>
        public async UniTask Attack()
        {
            await BattleManager.PlayerDamageWithText(1.0f);

            await UniTask.Delay(TimeSpan.FromSeconds(1.0f));
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
            await MessagePresenter.Instance.AppearMessage("スカイアは空を飛び始めた");
        }
        
        /// <inheritdoc/>
        public async UniTask Specific(int selfHeight, int selfWidth)
        {
            if (!_isFlying)
            {
                // 1ターン目：飛び上がる
                _isFlying = true;
            }
            else
            {
                // 2ターン目：ビーム攻撃（2倍ダメージ）
                await BattleManager.PlayerDamageWithText(SpecificDamageRate);

                await UniTask.Delay(TimeSpan.FromSeconds(1.0f));

                // 地上に戻る
                _isFlying = false;
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
