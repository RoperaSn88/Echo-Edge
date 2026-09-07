namespace EchoEdge.Domain.Battle
{
    public class DefendBuff : IBuff
    {
        private readonly BuffKinds _kind = BuffKinds.Defend;
        private const int Amount = 3;

        /// <summary>
        /// 防御力を3上昇させる
        /// </summary>
        public void Buff(BattleStatus targetStatus)
        {
            targetStatus.ChangeDefend(Amount);
        }

        /// <summary>
        /// 防御力バフを消す
        /// </summary>
        public void RemoveBuff(BattleStatus targetStatus)
        {
            targetStatus.ChangeDefend(-Amount);
        }

        public BuffKinds GetBuffKinds()
        {
            return _kind;
        }
    }
}
