using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;

/// <summary>
/// ステージのレベルを調節するスクリプト。
/// </summary>
public class StageLevelController : MonoBehaviour
{
    /// <summary>
    /// レベルを表示するテキスト
    /// </summary>
    [SerializeField]
    private TextMeshProUGUI _levelText;

    /// <summary>
    /// レベルを増加させるボタン（ClickableImage）
    /// </summary>
    [SerializeField]
    private ClickableImage _incrementButton;

    /// <summary>
    /// レベルを減少させるボタン（ClickableImage）
    /// </summary>
    [SerializeField]
    private ClickableImage _decrementButton;

    private ClickAction _incrementAction;
    private ClickAction _decrementAction;

    private void Start()
    {
        _incrementAction = _ => IncrementLevel();
        _decrementAction = _ => DecrementLevel();

        if (_incrementButton != null)
        {
            _incrementButton.OnClick += _incrementAction;
        }

        if (_decrementButton != null)
        {
            _decrementButton.OnClick += _decrementAction;
        }

        UpdateLevelText();
        UpdateButtonInteractable();
    }

    private void OnDestroy()
    {
        if (_incrementButton != null)
        {
            _incrementButton.OnClick -= _incrementAction;
        }

        if (_decrementButton != null)
        {
            _decrementButton.OnClick -= _decrementAction;
        }
    }

    /// <summary>
    /// レベルを1増加させてテキストを更新する
    /// </summary>
    public void IncrementLevel()
    {
        StageData.IncrementLevel();
        UpdateLevelText();
        UpdateButtonInteractable();
    }

    /// <summary>
    /// レベルを1減少させてテキストを更新する
    /// </summary>
    public void DecrementLevel()
    {
        StageData.DecrementLevel();
        UpdateLevelText();
        UpdateButtonInteractable();
    }

    private void UpdateLevelText()
    {
        if (_levelText != null)
        {
            _levelText.text = StageData.Level.ToString();
        }
    }

    /// <summary>
    /// 選択不可能な範囲へ進もうとするボタンを、押せない半透明な状態にする
    /// </summary>
    private void UpdateButtonInteractable()
    {
        if (_incrementButton != null)
        {
            _incrementButton.Interactable = StageData.Level < StageData.HighestClearedStage;
        }

        if (_decrementButton != null)
        {
            _decrementButton.Interactable = StageData.Level > StageData.MinLevel;
        }
    }
}
