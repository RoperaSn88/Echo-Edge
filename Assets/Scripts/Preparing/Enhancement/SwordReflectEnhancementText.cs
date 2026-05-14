/// <summary>
/// 剣の反射回数を強化するテキスト。
/// </summary>
public class SwordReflectEnhancementText : EnhancementItemText
{
    /// <inheritdoc/>
    protected override bool TryEnhance()
    {
        return EnhancementManager.TryUpgradeSwordReflect();
    }
}
