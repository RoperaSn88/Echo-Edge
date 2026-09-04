namespace EchoEdge.Domain.Battle
{
    public class MoveBuff : IBuff
    {
        private readonly BuffKinds _kind = BuffKinds.Move;

        /// <summary>
        /// 移動速度を1上昇させる
        /// </summary>
        public void Buff(BattleStatus targetStatus)
        {
            targetStatus.ChangeMove(1);
        }

        /// <summary>
        /// 移動速度バフを消す
        /// </summary>
        public void RemoveBuff(BattleStatus targetStatus)
        {
            targetStatus.ChangeMove(-1);
        }

        public BuffKinds GetBuffKinds()
        {
            return _kind;
        }
    }
}
