public interface IBuff
{
    /// <summary>
    /// バフを与えるためのメソッド
    /// </summary>
    /// <param name="targetStatus"></param>
    public void Buff(BattleStatus targetStatus);

    /// <summary>
    /// バフを消すためのメソッド
    /// </summary>
    /// <param name="targetStatus"></param>
    public void RemoveBuff(BattleStatus targetStatus);

    public BuffKinds GetBuffKinds();
}