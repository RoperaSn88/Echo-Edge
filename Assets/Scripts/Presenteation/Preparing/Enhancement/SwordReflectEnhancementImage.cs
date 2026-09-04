using EchoEdge.App.Preparing;

namespace EchoEdge.Presenter.Preparing
{
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
}
