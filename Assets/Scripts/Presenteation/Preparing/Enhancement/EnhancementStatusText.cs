using TMPro;
using UnityEngine;

/// <summary>
/// 強化画面上に現在の石の所持数と剣のステータスを表示するクラス。
/// </summary>
public class EnhancementStatusText : MonoBehaviour
{
    /// <summary>
    /// シングルトンインスタンス
    /// </summary>
    public static EnhancementStatusText Instance { get; private set; }

    /// <summary>
    /// 経験値・ステータスを表示するテキスト
    /// </summary>
    [SerializeField]
    private TextMeshProUGUI _statusText;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Debug.LogError($"{nameof(EnhancementStatusText)}: 複数のインスタンスが検出されました。重複インスタンスを削除します。");
            Destroy(gameObject);
            return;
        }
        Instance = this;
    }

    private void OnEnable()
    {
        RefreshText();
    }

    private void OnDestroy()
    {
        if (Instance == this)
        {
            Instance = null;
        }
    }

    /// <summary>
    /// 表示テキストを現在の石の所持数・剣のステータスで更新する。
    /// </summary>
    public void RefreshText()
    {
        if (_statusText == null) return;

        var sword = PlayerSwordParameterHolder.SwordStatus;
        _statusText.text =
            $"石: {EnhancementManager.Stone}\n" +
            $"剣攻撃力: {sword.Attack}\n反射回数: {sword.ReflectCount}";
    }
}
