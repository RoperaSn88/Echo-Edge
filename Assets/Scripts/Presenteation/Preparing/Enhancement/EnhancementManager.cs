using UnityEngine;

/// <summary>
/// 強化画面のロジックを管理するクラス。
/// 石（強化用通貨）を保持し、剣のステータス強化を行う。
/// </summary>
public static class EnhancementManager
{
    /// <summary>剣の攻撃力 1回の強化量</summary>
    public const int SwordAttackUpgradeAmount = 5;

    /// <summary>剣の反射回数 1回の強化量</summary>
    public const int SwordReflectUpgradeAmount = 1;

    /// <summary>強化 1回のコスト（石）</summary>
    public const int UpgradeCost = 1;

    private static int _stone;

    /// <summary>
    /// 現在の石の所持数
    /// </summary>
    public static int Stone => _stone;

    static EnhancementManager()
    {
        _stone = PlayerSwordParameterSaveManager.LoadStone();
    }

    /// <summary>
    /// 石を追加して永続化する。
    /// </summary>
    /// <param name="amount">追加する石の数</param>
    public static void AddStone(int amount)
    {
        if (amount < 0)
        {
            Debug.LogWarning($"{nameof(EnhancementManager)}: 負の石の数が渡されました ({amount})。無視します。");
            return;
        }
        if (amount == 0) return;
        _stone += amount;
        PlayerSwordParameterSaveManager.SaveStone(_stone);
    }

    /// <summary>
    /// 剣の攻撃力を強化する。
    /// 石が足りない場合は強化せずに false を返す。
    /// </summary>
    /// <returns>強化が成功したか</returns>
    public static bool TryUpgradeSwordAttack()
    {
        if (_stone < UpgradeCost)
        {
            return false;
        }

        _stone -= UpgradeCost;
        PlayerSwordParameterSaveManager.SaveStone(_stone);

        var current = PlayerSwordParameterHolder.SwordStatus;
        PlayerSwordParameterHolder.SetSwordStatus(
            current.HP,
            current.Attack + SwordAttackUpgradeAmount,
            current.ReflectCount
        );
        return true;
    }

    /// <summary>
    /// 剣の反射回数を強化する。
    /// 石が足りない場合は強化せずに false を返す。
    /// </summary>
    /// <returns>強化が成功したか</returns>
    public static bool TryUpgradeSwordReflect()
    {
        if (_stone < UpgradeCost)
        {
            return false;
        }

        _stone -= UpgradeCost;
        PlayerSwordParameterSaveManager.SaveStone(_stone);

        var current = PlayerSwordParameterHolder.SwordStatus;
        PlayerSwordParameterHolder.SetSwordStatus(
            current.HP,
            current.Attack,
            (byte)(current.ReflectCount + SwordReflectUpgradeAmount)
        );
        return true;
    }
}
