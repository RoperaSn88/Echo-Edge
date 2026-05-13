using Cysharp.Threading.Tasks;
using TMPro;
using UnityEngine;

/// <summary>
/// 強化画面の各ステータス強化項目を表示・操作するクラス。
/// 選択時に強化内容（現在値 → 強化後の値、コスト）を表示し、
/// 決定時に EnhancementManager 経由でステータスを強化する。
/// </summary>
public class EnhancementItemText : TMPSelectObject
{
    /// <summary>
    /// 強化するステータスの種類
    /// </summary>
    [SerializeField]
    private EnhancementKind _kind;

    /// <summary>
    /// 強化内容（現在値・強化量・コスト）を表示するテキスト
    /// </summary>
    [SerializeField]
    private TextMeshProUGUI _infoText;

    /// <summary>
    /// 強化成功 / 経験値不足を一時的に表示するテキスト
    /// </summary>
    [SerializeField]
    private TextMeshProUGUI _feedbackText;

    private void Start()
    {
        RefreshInfoText();
    }

    /// <summary>
    /// 強化内容テキストを現在のステータスで更新する。
    /// </summary>
    public void RefreshInfoText()
    {
        if (_infoText == null) return;

        var player = PlayerSwordParameterHolder.PlayerStatus;
        string label;
        int current;
        int amount;

        switch (_kind)
        {
            case EnhancementKind.HP:
                label = "HP";
                current = player.HP;
                amount = EnhancementManager.HpUpgradeAmount;
                break;
            case EnhancementKind.Attack:
                label = "攻撃力";
                current = player.Attack;
                amount = EnhancementManager.AttackUpgradeAmount;
                break;
            case EnhancementKind.Defend:
                label = "防御力";
                current = player.Defend;
                amount = EnhancementManager.DefendUpgradeAmount;
                break;
            default:
                return;
        }

        _infoText.text = $"{label}  {current} → {current + amount}\n(消費EXP: {EnhancementManager.UpgradeCost})";
    }

    /// <inheritdoc/>
    public override async UniTask OnDecide()
    {
        bool success = EnhancementManager.TryUpgrade(_kind);

        if (_feedbackText != null)
        {
            _feedbackText.text = success ? "強化成功！" : "EXPが足りません";
            _feedbackText.gameObject.SetActive(true);
        }

        if (success)
        {
            RefreshInfoText();
            EnhancementStatusText.Instance?.RefreshText();
        }

        await UniTask.Delay(System.TimeSpan.FromSeconds(0.8f));

        if (_feedbackText != null)
        {
            _feedbackText.gameObject.SetActive(false);
        }
    }
}
