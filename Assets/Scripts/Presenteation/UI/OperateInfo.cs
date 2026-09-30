using System.Threading;
using Cysharp.Threading.Tasks;
using UnityEngine;
using DG.Tweening;
using EchoEdge.Domain.Phase;

namespace EchoEdge.Presenter.UI
{
    [RequireComponent(typeof(CanvasGroup))]
    public class OperateInfo : MonoBehaviour
    {
        [SerializeField]
        private PhaseKinds _phaseKind;
        public PhaseKinds PhaseKind => _phaseKind;

        [SerializeField]
        private CanvasGroup _canvasGroup;

        [SerializeField]
        private RectTransform _rectTransform;
        
        private const float SlideDistance = 50f;

        private const float TweenDuration = 0.5f;

        private Vector2 _shownPosition;
        private Vector2 HiddenPosition => _shownPosition + Vector2.left * SlideDistance;

        private void Awake()
        {
            if (_rectTransform == null)
            {
                _rectTransform = (RectTransform)transform;
            }
            _shownPosition = _rectTransform.anchoredPosition;
            Hide();
        }

        /// <summary>
        /// トゥイーンを挟まずに即時非表示状態(透明・左側)にする
        /// </summary>
        public void Hide()
        {
            _canvasGroup.alpha = 0;
            _rectTransform.anchoredPosition = HiddenPosition;
        }

        /// <summary>
        /// 左側からスライドしながら出現する
        /// </summary>
        public UniTask OpenAsync(CancellationToken cancellationToken)
        {
            return DOTween.Sequence()
                .Join(_canvasGroup.DOFade(1, TweenDuration))
                .Join(_rectTransform.DOAnchorPos(_shownPosition, TweenDuration).SetEase(Ease.OutCubic))
                .ToUniTask(cancellationToken: cancellationToken);
        }

        /// <summary>
        /// 左へスライドしながら消える
        /// </summary>
        public UniTask CloseAsync(CancellationToken cancellationToken)
        {
            return DOTween.Sequence()
                .Join(_canvasGroup.DOFade(0, TweenDuration))
                .Join(_rectTransform.DOAnchorPos(HiddenPosition, TweenDuration).SetEase(Ease.InCubic))
                .ToUniTask(cancellationToken: cancellationToken);
        }
    }
}
