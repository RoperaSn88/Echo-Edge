public class MoveBuff : IBuff
{
    private readonly BuffKinds _kind = BuffKinds.Move;
    private readonly BattleStatus IncreaseStatus = new BattleStatus(0, 0, 0, 1, MovePattern.Invalid, 0, 0);

    /// <summary>
    /// 移動速度を1上昇させる
    /// </summary>
    public void Buff(BattleStatus targetStatus)
    {
        
    }

    /// <summary>
    /// 移動速度バフを消す
    /// </summary>
    public void RemoveBuff(BattleStatus targetStatus)
    {
        
    }

    public BuffKinds GetBuffKinds()
    {
        return _kind;
    }
}
