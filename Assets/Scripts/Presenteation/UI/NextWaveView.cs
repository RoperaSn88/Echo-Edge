using Cysharp.Threading.Tasks;
using DG.Tweening;
using UnityEngine;

using EchoEdge.Infra.Audio;

namespace EchoEdge.Presenter.UI
{
    public class NextWaveView: MonoBehaviour
    {
        public static NextWaveView Instance { get; private set; }

        [SerializeField]
        private CanvasGroup _canvasGroup;

        [SerializeField]
        private ParticleSystem _particleSystem;

        [SerializeField]
        private RectTransform _textRect;

        private const float TextMoveDistance = 960f;

        private const float ShowTime = 2f;

        void Start()
        {
            Instance = this;
            _canvasGroup.alpha = 0f;
            _canvasGroup.gameObject.SetActive(false);
            _particleSystem.Stop();
        }

        public async UniTask ShowNextWave() 
        {
            AudioManager.Instance.PlaySe(SeAudioType.NextWave);
            _particleSystem.Play();
            _canvasGroup.gameObject.SetActive(true);
            _textRect.anchoredPosition = new Vector2(TextMoveDistance * 1.5f, _textRect.anchoredPosition.y);

            await UniTask.WhenAll(
                _canvasGroup.DOFade(1f, 0.5f).ToUniTask(),
                _textRect.DOAnchorPosX(TextMoveDistance, ShowTime).SetEase(Ease.OutQuad).ToUniTask()
            );
        }

        public async UniTask HideNextWave() 
        {
            await UniTask.WhenAll(
                _canvasGroup.DOFade(0f, 0.5f).ToUniTask(),
                _textRect.DOAnchorPosX(TextMoveDistance/2, ShowTime).SetEase(Ease.InQuad).ToUniTask()
            );
            _canvasGroup.gameObject.SetActive(false);
            _particleSystem.Stop();
        }
    }
}
