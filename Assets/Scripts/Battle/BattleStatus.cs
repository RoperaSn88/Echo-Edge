using UnityEngine;

public class BattleStatus 
{
    /// <summary>
    /// 元のステータス
    /// </summary>
    private BaseStatus baseStatus;

    /// <summary>
    /// 体力
    /// </summary>
    private int hp;

    public int HP => hp;

    /// <summary>
    /// 攻撃力
    /// </summary>
    private int attack;

    public int Attack => attack;

    /// <summary>
    /// 防御力
    /// </summary>
    private int defend;

    public int Defend => defend;

    /// <summary>
    /// 特殊行動の発動順番 移動をする前か後か
    /// </summary>
    private MovePattern pattern;

    /// <summary>
    /// 1回の移動回数
    /// </summary>
    private int move;

    public void Damage(int damage)
    {
        hp -= damage;
        if(hp <= 0)
        {
            Debug.Log("死んだぜ");
        }
    }
}
