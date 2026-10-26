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

        [SerializeField, Tooltip("フラスコのアニメーター（無ければ着地と同時にプールへ戻る）")]
        private Animator _animator;

        [SerializeField, Min(0.01f), Tooltip("投げてから着地するまでの秒数")]
        private float _flightDuration = 0.6f;

        [SerializeField, Min(0f), Tooltip("放物線の高さ")]
        private float _jumpPower = 2f;

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

            // プールから再利用したときに、前回のアニメーションの状態が残らないようにする
            if (HasController())
            {
                _animator.Rebind();
                _animator.Update(0f);
            }

            await transform
                .DOJump(to, _jumpPower, 1, _flightDuration)
                .SetEase(Ease.Linear)
                .ToUniTask(cancellationToken: destroyCancellationToken);

            _hasLanded = true;

            if (!HasController())
            {
                ReleaseOnce();
                return;
            }

            if (HasTrigger(LandTrigger))
            {
                _animator.SetTrigger(LandTrigger);
            }

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

        private bool HasController()
        {
            return _animator != null && _animator.runtimeAnimatorController != null;
        }

        private bool HasTrigger(string name)
        {
            foreach (var parameter in _animator.parameters)
            {
                if (parameter.type == AnimatorControllerParameterType.Trigger && parameter.name == name)
                {
                    return true;
                }
            }
            return false;
        }
    }
}
