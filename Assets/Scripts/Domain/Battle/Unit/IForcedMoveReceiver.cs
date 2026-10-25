using Cysharp.Threading.Tasks;

namespace EchoEdge.Domain.Battle
{
    /// <summary>
    /// 装備品などの外部要因でマスを移動させられたときに反応するユニットを表すインターフェース
    /// </summary>
    public interface IForcedMoveReceiver
    {
        /// <summary>
        /// マグネティックコアによって実際にマスが変わったときに呼ばれる
        /// </summary>
        public UniTask OnPulledByMagneticCore();
    }
}
