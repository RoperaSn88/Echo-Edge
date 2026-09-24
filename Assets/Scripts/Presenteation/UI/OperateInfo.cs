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
        
        private const float FadeDuration = 0.5f;

        private void Awake()
        {
            SetAlpha(0);
        }

        /// <summary>
        /// フェードを挟まずに透明度を即時設定する
        /// </summary>
        public void SetAlpha(float alpha)
        {
            _canvasGroup.alpha = alpha;
        }

        public UniTask OpenAsync(CancellationToken cancellationToken)
        {
            return _canvasGroup.DOFade(1, FadeDuration).ToUniTask(cancellationToken: cancellationToken);
        }

        public UniTask CloseAsync(CancellationToken cancellationToken)
        {
            return _canvasGroup.DOFade(0, FadeDuration).ToUniTask(cancellationToken: cancellationToken);
        } 
    }
}