using UnityEngine;

public class HPBuff : IBuff
{
    private readonly BuffKinds kind = BuffKinds.HP;
    private const float HPbuff = 1.3f;
    public void Buff(BattleStatus targetStatus)
    {
        Debug.Log(targetStatus.HP * HPbuff);
        targetStatus.HP = (int)(targetStatus.HP * HPbuff);
    }

    public BuffKinds GetBuffKinds()
    {
        return kind;
    }
}