using System;
using System.Threading;
using Cysharp.Threading.Tasks;
using DG.Tweening;
using UnityEngine;

namespace EchoEdge.Presenter.Battle
{
    /// <summary>
    /// エナジーウォールの出現・消去の演出。エナジーウォールのプレハブのルート（<see cref="SlopeWallView"/> と同じ GameObject）に付ける。
    /// 出現時は三角柱（Body）が地面から生えるように伸び、消去時は地面へ沈むように縮む。どちらもパーティクルを再生できる。
    /// このコンポーネントが無いプレハブでは、従来どおり演出なしで即座に出現・消去する。
    /// </summary>
    [RequireComponent(typeof(SlopeWallView))]
    public class EnergyWallEffectView : MonoBehaviour
    {
        /// <summary>
        /// 縮めきったときの高さの割合。0 にすると Scale が 0 になり描画が不安定になることがあるため少しだけ残す
        /// </summary>
        private const float MinHeightRate = 0.001f;

        [Header("出現")]
        [SerializeField, Min(0f)]
        private float _appearDuration = 0.35f;

        [SerializeField]
        private Ease _appearEase = Ease.OutBack;

        [Tooltip("出現時に再生するパーティクル（任意）。Body ではなくルートの子に置く")]
        [SerializeField]
        private ParticleSystem _appearParticle;

        [Header("消去")]
        [SerializeField, Min(0f)]
        private float _disappearDuration = 0.3f;

        [SerializeField]
        private Ease _disappearEase = Ease.InBack;

        [Tooltip("消去時に再生するパーティクル（任意）。Body ではなくルートの子に置く")]
        [SerializeField]
        private ParticleSystem _disappearParticle;

        [Tooltip("消去時、パーティクルが消えきるまで待つ最大秒数。これを過ぎるとパーティクルが残っていてもプールへ戻す")]
        [SerializeField, Min(0f)]
        private float _maxDisappearParticleWait = 2f;

        [Header("メッシュ")]
        [Tooltip("Body のメッシュの、ローカル原点から底面までの高さ。EnergySlopeWall.obj は中心が原点で高さ 1 なので 0.5")]
        [SerializeField]
        private float _meshBottomOffset = 0.5f;
        
        [SerializeField]
        private Animator _bodyAnimator;

        private SlopeWallView _slopeWallView;
        private Transform _body;
        private Vector3 _baseBodyPosition;
        private Vector3 _baseBodyScale;
        private bool _hasBaseTransform;

        private Tween _tween;
        private CancellationTokenSource _disappearCts;

        /// <summary>
        /// 消去の演出中かどうか
        /// </summary>
        public bool IsDisappearing { get; private set; }

        private void Awake()
        {
            CacheBaseTransform();
        }

        private void OnDisable()
        {
            // プールへ戻されたときは、次に取り出されたときのために見た目を元に戻しておく
            ResetVisual();
        }

        private void OnDestroy()
        {
            CancelDisappear();
        }

        /// <summary>
        /// 地面から生える演出を再生する。
        /// </summary>
        public void PlayAppear()
        {
            ResetVisual();
            
            _bodyAnimator.ResetTrigger("AppearT");
            _bodyAnimator.SetTrigger("AppearT");

            if (_appearParticle != null)
            {
                _appearParticle.Clear(true);
                _appearParticle.Play(true);
            }

            if (_body == null || _appearDuration <= 0f) return;

            SetHeightRate(MinHeightRate);
            _tween = DOVirtual.Float(MinHeightRate, 1f, _appearDuration, SetHeightRate)
                .SetEase(_appearEase)
                .SetLink(gameObject);
        }

        /// <summary>
        /// 地面へ沈んで消える演出を再生し、終わったら <paramref name="onComplete"/> を呼ぶ。
        /// 当たり判定はすぐに無効にする。
        /// </summary>
        /// <param name="onComplete">演出が終わったときに呼ぶ処理（プールへ戻すなど）</param>
        public void PlayDisappear(Action onComplete)
        {
            CancelDisappear();
            KillTween();

            _slopeWallView ??= GetComponent<SlopeWallView>();
            _slopeWallView.SetColliderEnabled(false);

            if (_appearParticle != null)
            {
                _appearParticle.Stop(true, ParticleSystemStopBehavior.StopEmitting);
            }

            if (_disappearParticle != null)
            {
                _disappearParticle.Clear(true);
                _disappearParticle.Play(true);
            }

            IsDisappearing = true;
            _disappearCts = new CancellationTokenSource();
            DisappearAsync(onComplete, _disappearCts.Token).Forget();
        }

        private async UniTaskVoid DisappearAsync(Action onComplete, CancellationToken token)
        {
            try
            {
                if (_body != null && _disappearDuration > 0f)
                {
                    float startRate = CurrentHeightRate();
                    _tween = DOVirtual.Float(startRate, MinHeightRate, _disappearDuration, SetHeightRate)
                        .SetEase(_disappearEase)
                        .SetLink(gameObject);
                    await _tween.ToUniTask(cancellationToken: token);
                }

                // 沈みきったら本体は隠し、パーティクルが消えきるのを待つ
                if (_body != null)
                {
                    _body.gameObject.SetActive(false);
                }

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

            IsDisappearing = false;
            onComplete?.Invoke();
        }

        /// <summary>
        /// 演出を止め、Body の位置・大きさ・表示を元に戻す。
        /// </summary>
        private void ResetVisual()
        {
            CancelDisappear();
            KillTween();
            IsDisappearing = false;

            CacheBaseTransform();
            if (_body != null)
            {
                _body.gameObject.SetActive(true);
                _body.localPosition = _baseBodyPosition;
                _body.localScale = _baseBodyScale;
            }

            if (_appearParticle != null) _appearParticle.Clear(true);
            if (_disappearParticle != null) _disappearParticle.Clear(true);
        }

        private void CacheBaseTransform()
        {
            if (_hasBaseTransform) return;

            _slopeWallView ??= GetComponent<SlopeWallView>();
            _body = _slopeWallView != null ? _slopeWallView.Body : null;
            if (_body == null) return;

            _baseBodyPosition = _body.localPosition;
            _baseBodyScale = _body.localScale;
            _hasBaseTransform = true;
        }

        /// <summary>
        /// 底面の高さを保ったまま、Body の高さを元の <paramref name="rate"/> 倍にする（地面から生える／沈む見た目）。
        /// </summary>
        private void SetHeightRate(float rate)
        {
            if (_body == null) return;

            float baseHeight = _baseBodyScale.y;
            float scaleY = baseHeight * rate;
            _body.localScale = new Vector3(_baseBodyScale.x, scaleY, _baseBodyScale.z);

            // 底面 = 位置 - オフセット × Scale.Y が変わらないように Y 位置をずらす
            float bottom = _baseBodyPosition.y - _meshBottomOffset * baseHeight;
            _body.localPosition = new Vector3(
                _baseBodyPosition.x,
                bottom + _meshBottomOffset * scaleY,
                _baseBodyPosition.z);
        }

        private float CurrentHeightRate()
        {
            if (_body == null || Mathf.Approximately(_baseBodyScale.y, 0f)) return 1f;
            return Mathf.Clamp01(_body.localScale.y / _baseBodyScale.y);
        }

        private void KillTween()
        {
            if (_tween != null && _tween.IsActive())
            {
                _tween.Kill();
            }
            _tween = null;
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
