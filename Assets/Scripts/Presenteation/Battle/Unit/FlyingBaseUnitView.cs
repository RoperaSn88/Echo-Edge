using System;
using Cysharp.Threading.Tasks;
using DG.Tweening;
using UnityEngine;

using EchoEdge.Domain.Battle;

namespace EchoEdge.Presenter.Battle
{
    /// <summary>
    /// 飛行能力を持つユニット用の View。
    /// BaseUnitView を継承し、IFlyingUnitView を実装することで飛び上がり・ビーム攻撃アニメーションを提供する。
    /// </summary>
    public class FlyingBaseUnitView : BaseUnitView, IFlyingUnitView
    {
        /// <summary>
        /// 地上のY座標（飛行前の元の高さ）
        /// </summary>
        private float _groundY;

        private const float FlyHeight = 2f;
        private const float FlyTime = 0.5f;

        /// <summary>
        /// 着地時に戻すアニメーターの待機状態（Skya.controller の既定状態）
        /// </summary>
        private const string IdleStateName = "Base Layer.New State";

        /// <inheritdoc/>
        public override async UniTask Setup(int h, int w, EnemyKinds enemyID)
        {
            await base.Setup(h, w, enemyID);
            _groundY = transform.localPosition.y;
        }

        /// <inheritdoc/>
        public async UniTask WaitFlyAnim()
        {
            Animator.SetTrigger("SkillT");
            await transform.DOLocalMoveY(_groundY + FlyHeight, FlyTime).SetEase(Ease.OutQuad);
        }

        /// <inheritdoc/>
        public async UniTask WaitAnimAfterFlying()
        {
            // スカイア用のアニメーター
            Animator.SetTrigger("BeamT");
            await UniTask.WaitUntil(() => AnimationFlag);
            await transform.DOLocalMoveY(_groundY, FlyTime).SetEase(Ease.InQuad);
        }

        /// <inheritdoc/>
        public async UniTask WaitLandAnim()
        {
            // 溜め状態は BeamT でしか抜けないため、ビームを撃たずに待機状態へ直接戻す
            Animator.ResetTrigger("SkillT");
            Animator.ResetTrigger("BeamT");
            Animator.Play(IdleStateName, 0, 0f);
            Animator.SetTrigger("FallT");

            transform.DOKill();
            await transform.DOLocalMoveY(_groundY, FlyTime).SetEase(Ease.InQuad);
        }

        /// <inheritdoc/>
        public void PlayInitialAnim()
        {
            Animator.SetTrigger("InitialT");
        }
    }
}
