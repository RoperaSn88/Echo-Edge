public interface IBuff
{
    /// <summary>
    /// バフを与えるためのメソッド
    /// </summary>
    /// <param name="targetStatus"></param>
    public void Buff(BattleStatus targetStatus);

    public BuffKinds GetBuffKinds();
}