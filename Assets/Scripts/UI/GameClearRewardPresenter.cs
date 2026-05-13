using System;
using Cysharp.Threading.Tasks;
using DG.Tweening;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace UI
{
    public class GameClearRewardPresenter : MonoBehaviour
    {
        private const float SlideInDuration = 0.35f;
        private static readonly Vector2 PanelSize = new(360f, 120f);
        private static readonly Vector2 VisibleAnchoredPosition = new(24f, -140f);
        private static readonly Vector2 HiddenAnchoredPosition = new(-PanelSize.x - 24f, -140f);

        private static GameClearRewardPresenter _instance;

        private RectTransform _rectTransform;
        private CanvasGroup _canvasGroup;
        private TextMeshProUGUI _levelText;
        private TextMeshProUGUI _experienceText;

        public static GameClearRewardPresenter Instance
        {
            get
            {
                if (_instance != null)
                {
                    return _instance;
                }

                _instance = FindFirstObjectByType<GameClearRewardPresenter>();
                if (_instance != null)
                {
                    return _instance;
                }

                var gameObject = new GameObject(nameof(GameClearRewardPresenter));
                gameObject.layer = LayerMask.NameToLayer("UI");
                gameObject.AddComponent<RectTransform>();
                gameObject.AddComponent<CanvasGroup>();
                _instance = gameObject.AddComponent<GameClearRewardPresenter>();
                return _instance;
            }
        }

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

            _instance = this;
            EnsureInitialized();
        }

        public async UniTask ShowAsync(int level, int gainedExperience, int totalExperience)
        {
            EnsureInitialized();

            _levelText.text = $"LEVEL  {level}";
            _experienceText.text = $"EXP  +{gainedExperience}\nTOTAL {totalExperience}";

            _rectTransform.DOKill();
            _canvasGroup.DOKill();
            _rectTransform.anchoredPosition = HiddenAnchoredPosition;
            _canvasGroup.alpha = 1f;
            gameObject.SetActive(true);
            _rectTransform.SetAsLastSibling();

            await _rectTransform.DOAnchorPos(VisibleAnchoredPosition, SlideInDuration)
                .SetEase(Ease.OutQuad)
                .SetUpdate(true)
                .ToUniTask();
        }

        private void EnsureInitialized()
        {
            if (_rectTransform != null && _levelText != null && _experienceText != null)
            {
                return;
            }

            var parent = ResolveParent();
            if (parent == null)
            {
                throw new InvalidOperationException("GameClearRewardPresenter を配置する Canvas が見つかりません。");
            }

            _rectTransform = GetComponent<RectTransform>();
            _canvasGroup = GetComponent<CanvasGroup>();
            _rectTransform.SetParent(parent, false);
            _rectTransform.anchorMin = new Vector2(0f, 1f);
            _rectTransform.anchorMax = new Vector2(0f, 1f);
            _rectTransform.pivot = new Vector2(0f, 1f);
            _rectTransform.sizeDelta = PanelSize;
            _rectTransform.anchoredPosition = HiddenAnchoredPosition;

            var background = gameObject.GetComponent<Image>();
            if (background == null)
            {
                background = gameObject.AddComponent<Image>();
            }

            background.color = new Color(0.08f, 0.11f, 0.18f, 0.92f);

            _levelText = CreateText(
                "LevelText",
                new Vector2(0f, 1f),
                new Vector2(0f, 1f),
                new Vector2(18f, -16f),
                new Vector2(324f, 40f),
                34f);

            _experienceText = CreateText(
                "ExperienceText",
                new Vector2(0f, 1f),
                new Vector2(0f, 1f),
                new Vector2(18f, -56f),
                new Vector2(324f, 48f),
                24f);

            gameObject.SetActive(false);
        }

        private RectTransform ResolveParent()
        {
            if (PlayerStatusPresenter.Instance != null && PlayerStatusPresenter.Instance.transform.parent is RectTransform parent)
            {
                return parent;
            }

            var canvas = FindFirstObjectByType<Canvas>();
            return canvas != null ? canvas.GetComponent<RectTransform>() : null;
        }

        private TextMeshProUGUI CreateText(
            string objectName,
            Vector2 anchorMin,
            Vector2 anchorMax,
            Vector2 anchoredPosition,
            Vector2 sizeDelta,
            float fontSize)
        {
            var child = new GameObject(objectName);
            child.layer = gameObject.layer;

            var rectTransform = child.AddComponent<RectTransform>();
            rectTransform.SetParent(transform, false);
            rectTransform.anchorMin = anchorMin;
            rectTransform.anchorMax = anchorMax;
            rectTransform.pivot = new Vector2(0f, 1f);
            rectTransform.anchoredPosition = anchoredPosition;
            rectTransform.sizeDelta = sizeDelta;

            var text = child.AddComponent<TextMeshProUGUI>();
            text.font = TMP_Settings.defaultFontAsset;
            text.fontSize = fontSize;
            text.color = Color.white;
            text.alignment = TextAlignmentOptions.Left;
            text.enableWordWrapping = false;

            return text;
        }
    }
}
