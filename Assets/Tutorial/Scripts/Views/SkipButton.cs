using System;
using System.Threading;
using LitMotion;
using LitMotion.Extensions;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace CommonUI.Tutorial.Views
{
    /// <summary>
    /// 長押ししたらゲージがたまるボタン.
    /// </summary>
    [RequireComponent(typeof(Image))]
    public class SkipButton : MonoBehaviour, IPointerUpHandler, IPointerDownHandler, IPointerExitHandler
    {
        /// <summary>
        /// スキップまでに長押しする秒数.
        /// </summary>
        [SerializeField, Tooltip("スキップまでに長押しする秒数")]
        private float _duration;

        /// <summary>
        /// 長押し時にたまるゲージ.
        /// </summary>
        private Image _frame;

        /// <summary>
        /// スキップ時に発火するイベント.
        /// </summary>
        public event Action OnSkip;

        private MotionHandle _handle;

        private CancellationTokenSource _cts;

        private void Start()
        {
            _frame = GetComponent<Image>();
            _frame.fillAmount = 0;
        }

        void IPointerDownHandler.OnPointerDown(PointerEventData __)
        {
            if (_cts != null)
            {
                return;
            }
            _cts = new CancellationTokenSource();
            AnimateFrame(1);
            _ = SkipAsync(_cts.Token);
        }

        void IPointerUpHandler.OnPointerUp(PointerEventData _)
        {
            CancelSkip();
            AnimateFrame(0);
        }

        void IPointerExitHandler.OnPointerExit(PointerEventData _)
        {
            CancelSkip();
            AnimateFrame(0);
        }

        private void AnimateFrame(float target)
        {
            if (_handle.IsActive())
            {
                _handle.Cancel();
            }

            var animDuration = GetAnimDuration(target);

            _handle = LMotion.Create(_frame.fillAmount, target, animDuration)
                .BindToFillAmount(_frame)
                .AddTo(this);
        }

        private float GetAnimDuration(float target)
        {
            var current = _frame.fillAmount;
            var distance = Math.Abs(target - current);
            return _duration * distance;
        }

        private async Awaitable SkipAsync(CancellationToken token)
        {
            var time = _duration * (1.0f - _frame.fillAmount);

            await Awaitable.WaitForSecondsAsync(time, token);
            OnSkip?.Invoke();
            _frame.fillAmount = 0;
        }

        private void CancelSkip()
        {
            if (_cts == null)
            {
                return;
            }
            _cts.Cancel();
            _cts.Dispose();
            _cts = null;
        }

        private void OnDestroy()
        {
            if (_handle.IsActive())
            {
                _handle.Cancel();
            }
            CancelSkip();
        }
    }
}