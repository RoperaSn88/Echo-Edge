using System;
using UnityEngine;

public class BattleStatus : IDamagable
{
    /// <summary>
    /// 元のステータス
    /// </summary>
    private BaseStatus baseStatus;

    public  int hp;
    /// <summary>
    /// 体力
    /// </summary>

    public int attack;
    /// <summary>
    /// 攻撃力
    /// </summary>

    public int defend;
    /// <summary>
    /// 防御力
    /// </summary>
    public int Defend => defend;


    public BattleStatus(BaseStatus BaseStatus)
    {
        this.baseStatus = BaseStatus;
        Initialize();
    }

    private void Initialize()
    {
        hp = baseStatus.HP;
        attack = baseStatus.Attack;
        defend = baseStatus.Defend;

        // buffsがnullまたは空の場合でもエラーにならないように保護
        if(baseStatus.Buffs == null)
        {
            throw new NullReferenceException("buffsが指定されてないです");
        }

        if (baseStatus.Buffs.Count > 0)
        {

            Debug.Log("バフ中");
            foreach (var v in baseStatus.Buffs)
            {
                v.Buff(this);
            }
        }

        Debug.Log($"hp:{hp}, attack:{attack}, defend:{defend}");
    }

    public bool Damage(int damage)
    {
        hp -= damage;
        Debug.Log($"ダメージをうけた 残り:{hp}");
        if(hp <= 0)
        {
            Debug.Log("死んだぜ");
            return true;
        }
        return false;
    }
}
