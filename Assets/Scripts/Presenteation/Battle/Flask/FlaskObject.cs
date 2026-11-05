using System;
using Cysharp.Threading.Tasks;
using DG.Tweening;
using UnityEngine;

using EchoEdge.Infra.Audio;
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

        [SerializeField, Tooltip("飛んでいる間のアニメーション（フラスコに付いている Animator の既定ステート）を、滞空時間にちょうど収まる速さで再生する")]
        private bool _fitAnimationToFlight = true;

        [Header("着地演出")]
        [SerializeField, Tooltip("着地した位置に出すエフェクトのプレハブ（未設定なら出さない）")]
        private GameObject _landingEffectPrefab;

        [SerializeField, Tooltip("着地エフェクトの、着地位置からのずらし")]
        private Vector3 _landingEffectOffset = Vector3.zero;

        [SerializeField, Min(0f), Tooltip("着地エフェクトを消すまでの秒数。0 ならエフェクト内の ParticleSystem の長さから自動で決める")]
        private float _landingEffectLifetime = 0f;

        [SerializeField, Tooltip("着地したときに SE（BattleSeType.FlaskLanding）を鳴らす")]
        private bool _playLandingSe = true;

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



            await transform
                .DOJump(to, _jumpPower, 1, _flightDuration)
                .SetEase(Ease.Linear)
                .ToUniTask(cancellationToken: destroyCancellationToken);

            _hasLanded = true;

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
        /// 飛んでいる間のアニメーションを滞空時間に合わせる。
        /// 滞空時間を短くしても、アニメーションが途中で切れないようにするため。
        /// </summary>
        private void FitAnimationToFlight(Animator animator)
        {
            if (!_fitAnimationToFlight || animator == null || animator.runtimeAnimatorController == null) return;

            animator.speed = 1f;
            animator.Rebind();
            animator.Update(0f);

            float clipLength = animator.GetCurrentAnimatorStateInfo(0).length;
            if (clipLength <= 0f || float.IsInfinity(clipLength)) return;

            animator.speed = clipLength / _flightDuration;
        }

        /// <summary>
        /// 着地エフェクトと SE を出す
        /// </summary>
        private void PlayLandingEffect(Vector3 position)
        {
            if (_playLandingSe && AudioManager.Instance != null)
            {
                AudioManager.Instance.PlaySe(BattleSeType.FlaskLanding);
            }

            if (_landingEffectPrefab == null) return;

            // フラスコはすぐプールへ戻るので、エフェクトはフラスコの子にせず、プールの下に出す
            var effect = Instantiate(_landingEffectPrefab, position + _landingEffectOffset, _landingEffectPrefab.transform.rotation, transform.parent);
            Destroy(effect, GetLandingEffectLifetime(effect));
        }

        private float GetLandingEffectLifetime(GameObject effect)
        {
            if (_landingEffectLifetime > 0f) return _landingEffectLifetime;

            float lifetime = 0f;
            foreach (var particle in effect.GetComponentsInChildren<ParticleSystem>())
            {
                var main = particle.main;
                float particleLifetime = main.startDelay.constantMax + main.duration + main.startLifetime.constantMax;
                lifetime = Mathf.Max(lifetime, particleLifetime);
            }

            // ParticleSystem が無い・ループするエフェクトでも残り続けないようにする
            return lifetime > 0f ? Mathf.Min(lifetime, ReleaseTimeout) : ReleaseTimeout;
        }

        private void TryRelease()
        {
            if (_hasLanded && _isAnimationEnded)
            {
                ReleaseOnce();
            }
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
