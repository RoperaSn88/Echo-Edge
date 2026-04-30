public class MoveBuff : IBuff
{
    private readonly BuffKinds _kind = BuffKinds.Move;
    private const int MoveIncrease = 1;

    /// <summary>
    /// 移動速度を1上昇させる
    /// </summary>
    public void Buff(BattleStatus targetStatus)
    {
        targetStatus.Move += MoveIncrease;
    }

    /// <summary>
    /// 移動速度バフを消す
    /// </summary>
    public void RemoveBuff(BattleStatus targetStatus)
    {
        targetStatus.Move -= MoveIncrease;
    }

    public BuffKinds GetBuffKinds()
    {
        return _kind;
    }
}
