using System;
using System.Threading;
using Cysharp.Threading.Tasks;
using DG.Tweening;
using TMPro;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.EventSystems;

namespace EchoEdge.Presenter.UI
{
    /// <summary>
    /// フェーズ中の行動選択に使うボタン。
    /// クリック・カーソルが重なった時・離れた時の処理は外部から代入して使う。
    /// </summary>
    public class PhaseButton : MonoBehaviour, IPointerClickHandler, IPointerEnterHandler, IPointerExitHandler
    {
        [SerializeField]
        private RectTransform _rectTransform;
        public RectTransform RectTransform => _rectTransform;
        
        [SerializeField]
        private CanvasGroup _canvasGroup;
        
        [SerializeField]
        private TextMeshProUGUI _text;
        public TextMeshProUGUI Text => _text;

        private const float NonSelectableTextSize = 50;
        private const float TweenTime = 0.2f;
        private const float SelectableTextSize = 110;
        private const float NonSelectableHeight = 100f;
        private const float SelectableHeight = 200f;
        private const float NonSelectableAlpha = 0f;
        private const float SelectableAlpha = 1f;
        
        /// <summary>
        /// クリックされた時の処理
        /// </summary>
        public Action OnClick { get; set; }

        /// <summary>
        /// 入力を受け付けるかどうか。false の間は各処理を呼ばない
        /// </summary>
        public bool Interactable { get; set; } = true;

        private CancellationTokenSource _cancellationTokenSource;
        
        public void Initialize()
        {
            _canvasGroup.alpha = NonSelectableAlpha;
            _text.fontSize = NonSelectableTextSize;
            _rectTransform.SetSizeWithCurrentAnchors(RectTransform.Axis.Vertical, NonSelectableHeight);
        }

        public void OnPointerClick(PointerEventData eventData)
        {
            if (!Interactable) return;
            OnClick?.Invoke();
        }

        public void OnPointerEnter(PointerEventData eventData)
        {
            if (!Interactable) return;
            _cancellationTokenSource?.Cancel();
            _cancellationTokenSource = new CancellationTokenSource();
            OnPointerEnterAsync(_cancellationTokenSource.Token).Forget();
        }

        private async UniTask OnPointerEnterAsync(CancellationToken token)
        {
            var sizeTween = DOTween.To(() => _text.fontSize, x => _text.fontSize = x, SelectableTextSize, TweenTime).SetUpdate(true).ToUniTask(cancellationToken: token);
            var alphaTween = DOTween.To(() => _canvasGroup.alpha, x => _canvasGroup.alpha = x, SelectableAlpha, TweenTime).SetUpdate(true).ToUniTask(cancellationToken: token);
            var heightTween = DOTween.To(() => _rectTransform.sizeDelta.y, x => _rectTransform.SetSizeWithCurrentAnchors(RectTransform.Axis.Vertical, x), SelectableHeight, TweenTime).SetUpdate(true).ToUniTask(cancellationToken: token);
            await UniTask.WhenAll(sizeTween, alphaTween, heightTween);
        }

        public void OnPointerExit(PointerEventData eventData)
        {
            if (!Interactable) return;
            _cancellationTokenSource?.Cancel();
            _cancellationTokenSource = new CancellationTokenSource();
            OnPointerExitAsync(_cancellationTokenSource.Token).Forget();
        }
        
        private async UniTask OnPointerExitAsync(CancellationToken token)
        {
            var sizeTween = DOTween.To(() => _text.fontSize, x => _text.fontSize = x, NonSelectableTextSize, TweenTime).SetUpdate(true).ToUniTask(cancellationToken: token);
            var alphaTween = DOTween.To(() => _canvasGroup.alpha, x => _canvasGroup.alpha = x, NonSelectableAlpha, TweenTime).SetUpdate(true).ToUniTask(cancellationToken: token);
            var heightTween = DOTween.To(() => _rectTransform.sizeDelta.y, x => _rectTransform.SetSizeWithCurrentAnchors(RectTransform.Axis.Vertical, x), NonSelectableHeight, TweenTime).SetUpdate(true).ToUniTask(cancellationToken: token);
            await UniTask.WhenAll(sizeTween, alphaTween, heightTween);
        }
    }
}
