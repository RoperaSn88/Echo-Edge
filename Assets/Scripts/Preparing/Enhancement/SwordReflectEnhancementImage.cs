/// <summary>
/// 剣の反射回数を強化するクリッカブルイメージ。
/// </summary>
public class SwordReflectEnhancementImage : EnhancementItemImage
{
    /// <inheritdoc/>
    protected override bool TryEnhance()
    {
        return EnhancementManager.TryUpgradeSwordReflect();
    }
}
