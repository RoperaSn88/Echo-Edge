/// <summary>
/// 剣の攻撃力を強化するテキスト。
/// </summary>
public class SwordAttackEnhancementText : EnhancementItemText
{
    /// <inheritdoc/>
    protected override bool TryEnhance()
    {
        return EnhancementManager.TryUpgradeSwordAttack();
    }
}
