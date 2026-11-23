using EchoEdge.Presenter.Battle;

namespace EchoEdge.Domain.Battle
{
    /// <summary>
    /// 自分の View を使う（カメラで自分を映すなど）ユニットアクションを表すインターフェース。
    /// 行動の直前に <see cref="BaseUnit"/> から View が渡される。
    /// </summary>
    public interface IUnitViewAware
    {
        void SetView(IUnitView view);
    }
}
