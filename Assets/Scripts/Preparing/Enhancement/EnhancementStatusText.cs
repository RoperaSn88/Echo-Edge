using TMPro;
using UnityEngine;

/// <summary>
/// 強化画面上に現在の累積経験値とプレイヤーステータスを表示するクラス。
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
    /// 表示テキストを現在の経験値・ステータスで更新する。
    /// </summary>
    public void RefreshText()
    {
        if (_statusText == null) return;

        var player = PlayerSwordParameterHolder.PlayerStatus;
        _statusText.text =
            $"EXP: {EnhancementManager.Experience}\n" +
            $"HP: {player.HP}  攻撃力: {player.Attack}  防御力: {player.Defend}";
    }
}
