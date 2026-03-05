public class BaseStatus 
{
    /// <summary>
    /// お名前
    /// </summary>
    private string name;
    /// <summary>
    /// 体力
    /// </summary>
    private int hp;

    /// <summary>
    /// 攻撃力
    /// </summary>
    private int attack;

    /// <summary>
    /// 防御力
    /// </summary>
    private int defend;

    /// <summary>
    /// 特殊行動の発動順番 移動をする前か後か
    /// </summary>
    private MovePattern pattern;

    /// <summary>
    /// 1回の移動回数
    /// </summary>
    private int move;
}
