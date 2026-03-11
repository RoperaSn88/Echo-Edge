using System.Collections.Generic;
using System.Data;
using MessagePack;

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

    private IBuff[] buffs;

    public IReadOnlyList<IBuff> Buffs => buffs;

    public void Initialize(int h, int a, int d, int m)
    {
        hp = h;
        attack = a;
        defend = d;
    }
}
