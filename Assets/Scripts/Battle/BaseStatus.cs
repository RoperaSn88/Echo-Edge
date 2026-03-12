using System.Collections.Generic;
using System.Globalization;
using UnityEngine;

[System.Serializable]
public class BaseStatus 
{
    /// <summary>
    /// お名前
    /// </summary>
    [SerializeField]
    private string name;
    /// <summary>
    /// 体力
    /// </summary>
    [SerializeField]
    private int hp;
    public int HP => hp;

    /// <summary>
    /// 攻撃力
    /// </summary>
    [SerializeField]
    private int attack;
    public int Attack => attack;

    /// <summary>
    /// 防御力
    /// </summary>
    [SerializeField]
    private int defend;
    public int Defend => defend;

    /// <summary>
    /// 特殊行動の発動順番 移動をする前か後か
    /// </summary>
    private MovePattern pattern;

    /// <summary>
    /// 1回の移動回数
    /// </summary>
    [SerializeField]
    private int move;
    public int Move => move;
    
    private List<IBuff> buffs = new List<IBuff>();

    /// <summary>
    /// 外部からアクセスする際は常にnullでないリストを返す。
    /// </summary>
    public List<IBuff> Buffs => buffs;

    public void Initialize(int h, int a, int d, int m)
    {
        hp = h;
        attack = a;
        defend = d;
        move = m;
        // 初期化時にリストが作成されていることを保証
        if (buffs == null)
        {
            buffs = new List<IBuff>();
        }
    }

    public bool RegistarBuff(IBuff buff)
    {
        buffs.Add(buff);
        return true;
    }
}
