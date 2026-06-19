using System;
using Cysharp.Threading.Tasks;
using DG.Tweening;
using TMPro;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.UI;

namespace UI
{
    public class GameClearRewardPresenter : MonoBehaviour
    {
        private const float SlideInDuration = 0.5f;
        private Vector2 _basePosition;
        private Vector2 _offsetPosition;

        private static GameClearRewardPresenter _instance;
        
        [SerializeField]
        private RectTransform _rectTransform;
        
        [SerializeField]
        private CanvasGroup _canvasGroup;
        
        [SerializeField]
        private TextMeshProUGUI _levelText;
        
        [SerializeField]
        private TextMeshProUGUI _experienceText;

        public static GameClearRewardPresenter Instance => _instance;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetStatic()
        {
            _instance = null;
        }

        private void Awake()
        {
            if (_instance != null && _instance != this)
            {
                Destroy(gameObject);
                return;
            }
            
            _basePosition = _rectTransform.anchoredPosition;
            _offsetPosition = _rectTransform.anchoredPosition + new Vector2(-960f, 0);
            _instance = this;
            gameObject.SetActive(false);
        }

        public async UniTask ShowAsync(int level, int gainedExperience, int currentExperience)
        {
            _levelText.text = $"LEVEL  {level}";
            _experienceText.text = $"EXP  +{gainedExperience}\nNEXT {currentExperience} / 100";

            _rectTransform.DOKill();
            _canvasGroup.DOKill();
            _rectTransform.anchoredPosition = _offsetPosition;
            _canvasGroup.alpha = 1f;
            gameObject.SetActive(true);
            _rectTransform.SetAsLastSibling();

            await _rectTransform.DOAnchorPos(_basePosition, SlideInDuration)
                .SetEase(Ease.OutQuad)
                .SetUpdate(true)
                .ToUniTask();
        }

        public async UniTask CloseAsync()
        {
            Debug.Log("Start Close");
            await _canvasGroup.DOFade(0f, SlideInDuration).SetUpdate(true);
            Debug.Log("End Close");
        }
    }
}
