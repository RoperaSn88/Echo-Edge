using Cysharp.Threading.Tasks;

namespace EchoEdge.Domain.Battle
{
    /// <summary>
    /// 飛行状態を持つユニットアクションを表すインターフェース
    /// </summary>
    public interface IFlyingUnit
    {
        /// <summary>
        /// 現在飛行中かどうか
        /// </summary>
        bool IsFlying { get; }

        /// <summary>
        /// 飛び始めるときの待機
        /// </summary>
        /// <returns></returns>
        public UniTask WaitToFlyMessage();

        /// <summary>
        /// 飛んだ後、次のターンに使うアニメ
        /// </summary>
        /// <returns></returns>
        public UniTask WaitFlyingMessage();

        /// <summary>
        /// 飛行（溜め）状態を強制的に解除し、着陸状態に戻す
        /// </summary>
        public void CancelFlying();
    }
}
