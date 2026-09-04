namespace EchoEdge.Domain.Battle
{
    public class AttackBuff : IBuff
    {
        private readonly BuffKinds _kind = BuffKinds.Attack;
        private const int Amount = 5;

        /// <summary>
        /// 攻撃力を5上昇させる
        /// </summary>
        public void Buff(BattleStatus targetStatus)
        {
            targetStatus.ChangeAttack(Amount);
        }

        /// <summary>
        /// 攻撃力バフを消す
        /// </summary>
        public void RemoveBuff(BattleStatus targetStatus)
        {
            targetStatus.ChangeAttack(-Amount);
        }

        public BuffKinds GetBuffKinds()
        {
            return _kind;
        }
    }
}
