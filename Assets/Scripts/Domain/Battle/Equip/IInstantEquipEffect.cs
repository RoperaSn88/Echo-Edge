namespace EchoEdge.Domain.Battle
{
    /// <summary>
    /// 床マスを選ばず、選択した時点で効果を発揮する装備品のインターフェース。
    /// <see cref="EchoEdge.Domain.Phase.PlayerEquipPhase"/> が床マスの選択を飛ばす目印に使う。
    /// </summary>
    public interface IInstantEquipEffect : IEquipEffect
    {
    }
}
