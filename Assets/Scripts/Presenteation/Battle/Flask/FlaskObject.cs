using System;
using Cysharp.Threading.Tasks;
using DG.Tweening;
using UnityEngine;

using EchoEdge.Presenter.UI;

namespace EchoEdge.Presenter.Battle
{
    /// <summary>
    /// 『シーカー』が投げるフラスコ。プールから取り出され、指定した位置へ放物線を描いて飛ぶ。
    /// 着地とフラスコのアニメーションの完了がそろったらプールへ戻る。
    /// </summary>
    public class FlaskObject : ObjectPooler
    {
        /// <summary>
        /// 着地したときに引くトリガー（割れる・開くなどのアニメーション用）
        /// </summary>
        private const string LandTrigger = "LandT";

        /// <summary>
        /// アニメーションイベントが来なかったときに、着地からこの秒数でプールへ戻す
        /// </summary>
        private const float ReleaseTimeout = 5f;

        [SerializeField, Min(0.01f), Tooltip("投げてから着地するまでの秒数")]
        private float _flightDuration = 0.6f;

        [SerializeField, Min(0f), Tooltip("放物線の高さ")]
        private float _jumpPower = 2f;

        [Header("着地演出")]
        [SerializeField, Tooltip("着地したときに再生するパーティクル（未設定なら出さない）。フラスコの子ならその場で再生し、プレハブならフラスコが最後にいた位置に生成する")]
        private ParticleSystem _landingParticle;

        private bool _hasLanded;
        private bool _isAnimationEnded;
        private bool _isReleased;

        /// <inheritdoc/>
        public override async UniTask Appear()
        {
            gameObject.SetActive(false);
            await UniTask.CompletedTask;
        }

        /// <summary>
        /// from から to へフラスコを投げる。着地したら完了する（プールへ戻るのはアニメーションの完了後）。
        /// </summary>
        public async UniTask ThrowAsync(Vector3 from, Vector3 to)
        {
            _hasLanded = false;
            _isAnimationEnded = false;
            _isReleased = false;

            transform.position = from;
            gameObject.SetActive(true);

            // プールから再利用したときに、前回の着地パーティクルが残らないようにする
            if (IsLandingParticleChild())
            {
                _landingParticle.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
            }

            await transform
                .DOJump(to, _jumpPower, 1, _flightDuration)
                .SetEase(Ease.Linear)
                .ToUniTask(cancellationToken: destroyCancellationToken);

            _hasLanded = true;
            PlayLandingParticle();

            TryRelease();
            ReleaseAfterTimeout().Forget();
        }

        /// <summary>
        /// フラスコのアニメーションの最後にアニメーションイベントから呼ぶ
        /// </summary>
        public void OnFlaskAnimationEnd()
        {
            _isAnimationEnded = true;
            TryRelease();
        }

        /// <summary>
        /// フラスコが最後にいた位置（着地位置）で着地パーティクルを再生する
        /// </summary>
        private void PlayLandingParticle()
        {
            if (_landingParticle == null) return;

            if (IsLandingParticleChild())
            {
                _landingParticle.Play(true);
                return;
            }

            // プレハブの場合: フラスコはプールへ戻るので、フラスコの子にせずプールの下に出し、再生し終わったら消す
            var particle = Instantiate(_landingParticle, transform.position, _landingParticle.transform.rotation, transform.parent);
            particle.Play(true);
            Destroy(particle.gameObject, GetParticleLifetime(particle));
        }

        /// <summary>
        /// 着地パーティクルがフラスコ自身の子（プレハブ内のオブジェクト）か
        /// </summary>
        private bool IsLandingParticleChild()
        {
            return _landingParticle != null && _landingParticle.transform.IsChildOf(transform);
        }

        private static float GetParticleLifetime(ParticleSystem root)
        {
            float lifetime = 0f;
            foreach (var particle in root.GetComponentsInChildren<ParticleSystem>())
            {
                var main = particle.main;
                lifetime = Mathf.Max(lifetime, main.startDelay.constantMax + main.duration + main.startLifetime.constantMax);
            }

            // ループするパーティクルでも残り続けないようにする
            return lifetime > 0f ? Mathf.Min(lifetime, ReleaseTimeout) : ReleaseTimeout;
        }

        private void TryRelease()
        {
            if (_hasLanded && _isAnimationEnded)
            {
                ReleaseAfterLandingParticleAsync().Forget();
            }
        }

        /// <summary>
        /// 着地パーティクルがフラスコの子なら、再生し終わるまで待ってからプールへ戻す（途中で消えないように）
        /// </summary>
        private async UniTaskVoid ReleaseAfterLandingParticleAsync()
        {
            if (IsLandingParticleChild())
            {
                await UniTask.WaitWhile(() => !_isReleased && _landingParticle.IsAlive(true), cancellationToken: destroyCancellationToken);
            }
            ReleaseOnce();
        }

        private async UniTaskVoid ReleaseAfterTimeout()
        {
            await UniTask.Delay(TimeSpan.FromSeconds(ReleaseTimeout), cancellationToken: destroyCancellationToken);
            if (_isReleased) return;

            Debug.LogWarning($"{nameof(FlaskObject)}: アニメーションイベント {nameof(OnFlaskAnimationEnd)} が来なかったため、プールへ戻します。");
            ReleaseOnce();
        }

        private void ReleaseOnce()
        {
            if (_isReleased) return;
            _isReleased = true;
            Release();
        }
    }
}
