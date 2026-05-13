using UnityEngine;

/// <summary>
/// 強化画面のロジックを管理するクラス。
/// 累積経験値を保持し、ステータス強化を行う。
/// </summary>
public static class EnhancementManager
{
    /// <summary>HP 1回の強化量</summary>
    public const int HpUpgradeAmount = 10;

    /// <summary>攻撃力 1回の強化量</summary>
    public const int AttackUpgradeAmount = 5;

    /// <summary>防御力 1回の強化量</summary>
    public const int DefendUpgradeAmount = 5;

    /// <summary>強化 1回のコスト（経験値）</summary>
    public const int UpgradeCost = 50;

    private static int _experience;

    /// <summary>
    /// 現在の累積経験値
    /// </summary>
    public static int Experience => _experience;

    static EnhancementManager()
    {
        _experience = EnhancementSaveManager.LoadExperience();
    }

    /// <summary>
    /// 経験値を追加して永続化する。
    /// </summary>
    /// <param name="amount">追加する経験値量</param>
    public static void AddExperience(int amount)
    {
        if (amount < 0)
        {
            Debug.LogWarning($"{nameof(EnhancementManager)}: 負の経験値が渡されました ({amount})。無視します。");
            return;
        }
        if (amount == 0) return;
        _experience += amount;
        EnhancementSaveManager.SaveExperience(_experience);
    }

    /// <summary>
    /// 指定したステータスを強化する。
    /// 経験値が足りない場合は強化せずに false を返す。
    /// </summary>
    /// <param name="kind">強化するステータスの種類</param>
    /// <returns>強化が成功したか</returns>
    public static bool TryUpgrade(EnhancementKind kind)
    {
        if (_experience < UpgradeCost)
        {
            return false;
        }

        _experience -= UpgradeCost;
        EnhancementSaveManager.SaveExperience(_experience);

        var current = PlayerSwordParameterHolder.PlayerStatus;
        PlayerParameter upgraded;
        switch (kind)
        {
            case EnhancementKind.HP:
                upgraded = new PlayerParameter(current.HP + HpUpgradeAmount, current.Attack, current.Defend);
                break;
            case EnhancementKind.Attack:
                upgraded = new PlayerParameter(current.HP, current.Attack + AttackUpgradeAmount, current.Defend);
                break;
            case EnhancementKind.Defend:
                upgraded = new PlayerParameter(current.HP, current.Attack, current.Defend + DefendUpgradeAmount);
                break;
            default:
                Debug.LogWarning($"{nameof(EnhancementManager)}: 未知の {nameof(EnhancementKind)} '{kind}'");
                return false;
        }

        PlayerSwordParameterHolder.SetPlayerStatus(upgraded);
        return true;
    }
}
