/// <summary>
/// 剣の攻撃力を強化するクリッカブルイメージ。
/// </summary>
public class SwordAttackEnhancementImage : EnhancementItemImage
{
    /// <inheritdoc/>
    protected override bool TryEnhance()
    {
        return EnhancementManager.TryUpgradeSwordAttack();
    }
}
