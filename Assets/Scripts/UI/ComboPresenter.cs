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

    private const float PopScale = 1.5f;
    private const float AppearDuration = 0.3f;
    private const float DisappearDuration = 0.3f;
    private const float BaseComboValue = 1.0f;
    private const float ComboStep = 0.1f;

    private void Awake()
    {
        Instance = this;
        _canvasGroup = GetComponent<CanvasGroup>();
        gameObject.SetActive(false);
    }

    /// <summary>
    /// コンボ数を更新して表示する
    /// </summary>
    /// <param name="combo">現在のコンボ数</param>
    public void SetCombo(int combo)
    {
        // 実行中のトゥイーンをキャンセル
        DOTween.Kill(transform);
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

        // でかい状態から元の大きさになるトゥイーン
        transform.localScale = Vector3.one * PopScale;
        transform.DOScale(Vector3.one, AppearDuration).SetEase(Ease.OutBack);
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
            transform.DOScale(Vector3.zero, DisappearDuration).SetEase(Ease.InQuad).ToUniTask(),
            _canvasGroup.DOFade(0f, DisappearDuration).ToUniTask()
        );

        gameObject.SetActive(false);
    }
}
