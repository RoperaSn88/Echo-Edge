using TMPro;
using UnityEngine;
using DG.Tweening;
using Cysharp.Threading.Tasks;

/// <summary>
/// コンボ数を画面上に表示するプレゼンター
/// </summary>
[RequireComponent(typeof(CanvasGroup))]
public class ComboPresenter : MonoBehaviour
{
    public static ComboPresenter Instance;

    [SerializeField, Tooltip("コンボ数を表示するテキスト")]
    private TextMeshProUGUI _comboText;

    [SerializeField, Tooltip("ダメージ倍率を表示するテキスト")]
    private TextMeshProUGUI _multiplierText;

    private CanvasGroup _canvasGroup;
    private RectTransform _rectTransform;

    // 通常時のサイズとフォントサイズ（Awake で記録）
    private Vector2 _normalSize;
    private float _normalFontSize;

    private const float PopScale = 1.5f;
    private const float AppearDuration = 0.3f;
    private const float DisappearDuration = 0.3f;
    private const float BaseComboValue = 1.0f;
    private const float ComboStep = 0.1f;

    private void Awake()
    {
        Instance = this;
        _canvasGroup = GetComponent<CanvasGroup>();
        _rectTransform = GetComponent<RectTransform>();
        _normalSize = _rectTransform.sizeDelta;
        _normalFontSize = _comboText.fontSize;
        gameObject.SetActive(false);
    }

    /// <summary>
    /// コンボ数を更新して表示する
    /// </summary>
    /// <param name="combo">現在のコンボ数</param>
    public void SetCombo(int combo)
    {
        // 実行中のトゥイーンをキャンセル
        DOTween.Kill(_rectTransform);
        DOTween.Kill(_comboText);
        DOTween.Kill(_canvasGroup);

        gameObject.SetActive(true);
        _canvasGroup.alpha = 1f;

        _comboText.text = $"{combo} COMBO";

        // ダメージ倍率テキストを更新
        float comboValue = BaseComboValue + (combo - 1) * ComboStep;
        float multiplier = comboValue * comboValue;
        if (_multiplierText != null)
        {
            _multiplierText.text = $"×{multiplier:F2}";
        }

        // でかい状態から元の大きさになるトゥイーン（rect.width/height と fontSize を使用）
        _rectTransform.sizeDelta = _normalSize * PopScale;
        _comboText.fontSize = _normalFontSize * PopScale;
        _rectTransform.DOSizeDelta(_normalSize, AppearDuration).SetEase(Ease.OutBack);
        _comboText.DOFontSize(_normalFontSize, AppearDuration).SetEase(Ease.OutBack);
    }

    /// <summary>
    /// コンボ表示をリセットして非表示にする
    /// </summary>
    public void ResetCombo()
    {
        ResetComboAsync().Forget();
    }

    private async UniTaskVoid ResetComboAsync()
    {
        // 小さくなるトゥイーンと、完全に透明になるトゥイーン
        await UniTask.WhenAll(
            _rectTransform.DOSizeDelta(Vector2.zero, DisappearDuration).SetEase(Ease.InQuad).ToUniTask(),
            _comboText.DOFontSize(0, DisappearDuration).SetEase(Ease.InQuad).ToUniTask(),
            _canvasGroup.DOFade(0f, DisappearDuration).ToUniTask()
        );

        gameObject.SetActive(false);
    }
}
