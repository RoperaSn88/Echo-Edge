using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// ステージのレベルを調節するスクリプト。
/// Imageオブジェクトに追加して使用する。
/// </summary>
[RequireComponent(typeof(Image))]
public class StageLevelController : MonoBehaviour
{
    /// <summary>
    /// レベルを表示するテキスト
    /// </summary>
    [SerializeField]
    private TextMeshProUGUI _levelText;

    private void Start()
    {
        UpdateLevelText();
    }

    /// <summary>
    /// レベルを1増加させてテキストを更新する
    /// </summary>
    public void IncrementLevel()
    {
        StageData.IncrementLevel();
        UpdateLevelText();
    }

    /// <summary>
    /// レベルを1減少させてテキストを更新する
    /// </summary>
    public void DecrementLevel()
    {
        StageData.DecrementLevel();
        UpdateLevelText();
    }

    private void UpdateLevelText()
    {
        if (_levelText != null)
        {
            _levelText.text = StageData.Level.ToString();
        }
    }
}
