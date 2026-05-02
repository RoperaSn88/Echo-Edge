/// <summary>
/// 飛行状態を持つユニットアクションを表すインターフェース
/// </summary>
public interface IFlyingUnit
{
    /// <summary>
    /// 現在飛行中かどうか
    /// </summary>
    bool IsFlying { get; }
}
