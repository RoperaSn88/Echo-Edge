namespace EchoEdge.Domain.Battle
{
    /// <summary>
    /// 効果中は致死ダメージを受けてもHPを1残す（食いしばり）バフ
    /// </summary>
    public class SurviveLethalBuff : IBuff
    {
        private readonly BuffKinds _kind = BuffKinds.SurviveLethal;

        public void Buff(BattleStatus targetStatus)
        {
            targetStatus.SurviveLethal = true;
        }

        public void RemoveBuff(BattleStatus targetStatus)
        {
            targetStatus.SurviveLethal = false;
        }

        public BuffKinds GetBuffKinds()
        {
            return _kind;
        }
    }
}
