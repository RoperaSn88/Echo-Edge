/// <summary>
/// ダメージ計算を担うドメインサービス。
/// 計算式をエンティティから分離することで、変更を一か所に集約する。
/// </summary>
public static class DamageCalculationService
{
    /// <summary>
    /// ダメージ量を計算する。
    /// 計算式: 攻撃力 - 防御力 / 2。結果が負になる場合は 0 を返す。
    /// </summary>
    /// <param name="attackPower">攻撃側の攻撃力</param>
    /// <param name="defenseValue">防御側の防御力</param>
    /// <returns>実際に与えるダメージ量（0以上）</returns>
    public static int Calculate(int attackPower, int defenseValue)
    {
        int damage = attackPower - defenseValue / 2;
        return damage < 0 ? 0 : damage;
    }
}
