using TMPro;
using UnityEngine;

/// <summary>
/// コンボ数を画面上に表示するプレゼンター
/// </summary>
public class ComboPresenter : MonoBehaviour
{
    public static ComboPresenter Instance;

    [SerializeField, Tooltip("コンボ数を表示するテキスト")]
    private TextMeshProUGUI _comboText;

    private void Awake()
    {
        Instance = this;
        gameObject.SetActive(false);
    }

    /// <summary>
    /// コンボ数を更新して表示する
    /// </summary>
    /// <param name="combo">現在のコンボ数</param>
    public void SetCombo(int combo)
    {
        gameObject.SetActive(true);
        _comboText.text = $"{combo} COMBO";
    }

    /// <summary>
    /// コンボ表示をリセットして非表示にする
    /// </summary>
    public void ResetCombo()
    {
        _comboText.text = "";
        gameObject.SetActive(false);
    }
}
