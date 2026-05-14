using Cysharp.Threading.Tasks;
using DG.Tweening;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;

/// <summary>
/// ClickableImage に対応した強化項目の基底クラス。
/// クリック時に強化処理を実行し、Overseer の Magic アニメーションを再生する。
/// 具体的な強化処理はサブクラスで実装する。
/// </summary>
[RequireComponent(typeof(ClickableImage))]
public abstract class EnhancementItemImage : MonoBehaviour
{
    private const string MagicTrigger = "MagicT";
    private const float SlideDuration = 0.3f;
    private const float SlideOffsetX = -400f;

    /// <summary>
    /// Overseer の Animator（インスペクターから設定する）
    /// </summary>
    [SerializeField]
    protected Animator _overseerAnimator;

    /// <summary>
    /// 強化成功 / 石不足を一時的に表示するテキスト
    /// </summary>
    [SerializeField]
    protected TextMeshProUGUI _feedbackText;

    private Vector2 _feedbackDefaultPos;
    private ClickableImage _clickableImage;
    private bool _isProcessing;

    private void Awake()
    {
        _clickableImage = GetComponent<ClickableImage>();
    }

    private void Start()
    {
        if (_feedbackText == null)
        {
            Debug.LogError($"{GetType().Name}: フィードバックテキストが設定されていません。インスペクターで _feedbackText を設定してください。");
            return;
        }
        _feedbackDefaultPos = _feedbackText.rectTransform.anchoredPosition;
        _feedbackText.gameObject.SetActive(false);
    }

    private void OnEnable()
    {
        if (_clickableImage != null)
        {
            _clickableImage.OnClick += HandleClick;
        }
    }

    private void OnDisable()
    {
        if (_clickableImage != null)
        {
            _clickableImage.OnClick -= HandleClick;
        }
    }

    private void HandleClick(PointerEventData eventData)
    {
        if (_isProcessing) return;
        _isProcessing = true;
        OnClickEnhance().Forget();
    }

    private async UniTaskVoid OnClickEnhance()
    {
        if (_feedbackText == null)
        {
            Debug.LogError($"{GetType().Name}: フィードバックテキストが設定されていません。");
            _isProcessing = false;
            return;
        }

        bool success = TryEnhance();

        _feedbackText.text = success ? "強化成功" : "石が足りません";

        // 画面左から登場するトゥイーン
        _feedbackText.rectTransform.anchoredPosition = new Vector2(_feedbackDefaultPos.x + SlideOffsetX, _feedbackDefaultPos.y);
        _feedbackText.gameObject.SetActive(true);
        await _feedbackText.rectTransform
            .DOAnchorPos(_feedbackDefaultPos, SlideDuration)
            .SetEase(Ease.OutQuad)
            .ToUniTask();

        if (success)
        {
            if (_overseerAnimator != null)
            {
                _overseerAnimator.SetTrigger(MagicTrigger);
            }
            else
            {
                Debug.LogWarning($"{GetType().Name}: Overseer の Animator が設定されていません。");
            }

            OnEnhanceSuccess();
        }

        await UniTask.Delay(System.TimeSpan.FromSeconds(0.8f));

        _feedbackText.gameObject.SetActive(false);
        _isProcessing = false;
    }

    /// <summary>
    /// 強化を試みる。石が足りなければ false を返す。
    /// </summary>
    /// <returns>強化に成功したか</returns>
    protected abstract bool TryEnhance();

    /// <summary>
    /// 強化成功時に呼ばれる追加処理（UI 更新等）。
    /// </summary>
    protected virtual void OnEnhanceSuccess()
    {
        EnhancementStatusText.Instance?.RefreshText();
    }
}
