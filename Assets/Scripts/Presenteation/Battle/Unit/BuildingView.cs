using System;
using System.Threading;
using Cysharp.Threading.Tasks;
using UnityEngine;

namespace EchoEdge.Presenter.Battle
{
    public class BuildingView : MonoBehaviour
    {
        private static readonly int AppearTrigger = Animator.StringToHash("AppearT");

        [Header("出現")]
        [Tooltip("出現時に AppearT トリガーを送る Animator（任意）。未設定なら演出なしで出現する")]
        [SerializeField]
        private Animator _animator;

        [Tooltip("出現時に再生するパーティクル（任意）。Play On Awake は切っておく")]
        [SerializeField]
        private ParticleSystem _appearParticle;

        [Header("消去")]
        [Tooltip("消去時に再生するパーティクル（任意）。Play On Awake は切っておく")]
        [SerializeField]
        private ParticleSystem _disappearParticle;

        [Tooltip("消去時、パーティクルが消えきるまで待つ最大秒数。これを過ぎるとパーティクルが残っていてもプールへ戻す")]
        [SerializeField, Min(0f)]
        private float _maxDisappearParticleWait = 2f;

        private MeshRenderer[] _meshRenderers;
        private Collider[] _colliders;
        private bool _isHidden;
        private CancellationTokenSource _disappearCts;

        private void OnDisable()
        {
            // プールへ戻されたときは、次に取り出されたときのために見た目を元に戻しておく
            ResetVisual();
        }

        private void OnDestroy()
        {
            CancelDisappear();
        }

        public void Set(int h, int w)
        {
            Vector3 vec = new Vector3(w, 0.25f, h);
            transform.localPosition = vec;
        }

        /// <summary>
        /// 出現アニメーションとパーティクルを再生する。
        /// </summary>
        public void PlayAppear()
        {
            ResetVisual();

            if (_animator != null)
            {
                _animator.ResetTrigger(AppearTrigger);
                _animator.SetTrigger(AppearTrigger);
            }

            if (_appearParticle != null)
            {
                _appearParticle.Clear(true);
                _appearParticle.Play(true);
            }
        }

        /// <summary>
        /// 壁の見た目と当たり判定をすぐに消してパーティクルを再生し、消えきったら <paramref name="onComplete"/> を呼ぶ。
        /// </summary>
        /// <param name="onComplete">演出が終わったときに呼ぶ処理（プールへ戻すなど）</param>
        public void PlayDisappear(Action onComplete)
        {
            CancelDisappear();
            SetWallVisible(false);

            if (_appearParticle != null)
            {
                _appearParticle.Stop(true, ParticleSystemStopBehavior.StopEmitting);
            }

            if (_disappearParticle != null)
            {
                _disappearParticle.Clear(true);
                _disappearParticle.Play(true);
            }

            _disappearCts = new CancellationTokenSource();
            DisappearAsync(onComplete, _disappearCts.Token).Forget();
        }

        private async UniTaskVoid DisappearAsync(Action onComplete, CancellationToken token)
        {
            try
            {
                if (_disappearParticle != null && _maxDisappearParticleWait > 0f)
                {
                    float elapsed = 0f;
                    while (_disappearParticle != null && _disappearParticle.IsAlive(true) && elapsed < _maxDisappearParticleWait)
                    {
                        await UniTask.Yield(PlayerLoopTiming.Update, token);
                        elapsed += Time.deltaTime;
                    }
                }
            }
            catch (OperationCanceledException)
            {
                return;
            }

            onComplete?.Invoke();
        }

        /// <summary>
        /// 演出を止め、壁の表示と当たり判定を元に戻す。
        /// </summary>
        private void ResetVisual()
        {
            CancelDisappear();

            if (_isHidden)
            {
                SetWallVisible(true);
            }

            if (_appearParticle != null) _appearParticle.Clear(true);
            if (_disappearParticle != null) _disappearParticle.Clear(true);
        }

        /// <summary>
        /// 壁本体のメッシュと当たり判定を切り替える。パーティクルの描画には影響しない。
        /// </summary>
        private void SetWallVisible(bool isVisible)
        {
            _meshRenderers ??= GetComponentsInChildren<MeshRenderer>(true);
            _colliders ??= GetComponentsInChildren<Collider>(true);

            foreach (var meshRenderer in _meshRenderers)
            {
                meshRenderer.enabled = isVisible;
            }

            foreach (var wallCollider in _colliders)
            {
                wallCollider.enabled = isVisible;
            }

            _isHidden = !isVisible;
        }

        private void CancelDisappear()
        {
            if (_disappearCts == null) return;
            _disappearCts.Cancel();
            _disappearCts.Dispose();
            _disappearCts = null;
        }
    }
}
