using UnityEngine;

public class BattleStatus 
{
    /// <summary>
    /// 元のステータス
    /// </summary>
    private BaseStatus baseStatus;

    private int hp;
    /// <summary>
    /// 体力
    /// </summary>
    public int HP => hp;

    private int attack;
    /// <summary>
    /// 攻撃力
    /// </summary>
    public int Attack => attack;

    private int defend;
    /// <summary>
    /// 防御力
    /// </summary>
    public int Defend => defend;

    /// <summary>
    /// 特殊行動の発動順番 移動をする前か後か
    /// </summary>
    private MovePattern pattern;

    private int move;
    /// <summary>
    /// 1回の移動回数
    /// </summary>
    public int Move => move;

    public void Initialize(int h, int a, int d, int m)
    {
        hp = h;
        attack = a;
        defend = d;
        move = m;
    }

    public bool Damage(int damage)
    {
        hp -= damage;
        if(hp <= 0)
        {
            Debug.Log("死んだぜ");
            return true;
        }
        return false;
    }
}
