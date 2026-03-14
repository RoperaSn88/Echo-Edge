using System;
using UnityEngine;

public class BattleStatus : IDamagable
{
    /// <summary>
    /// 元のステータス
    /// </summary>
    private BaseStatus baseStatus;

    /// <summary>
    /// 体力
    /// </summary>
    public int HP;

    /// <summary>
    /// 攻撃力
    /// </summary>
    public int Attack;

    /// <summary>
    /// 防御力
    /// </summary>
    public int Defend;
    
    /// <summary>
    /// 1ターンの移動回数
    /// </summary>
    public int Move;
    


    public BattleStatus(BaseStatus BaseStatus)
    {
        this.baseStatus = BaseStatus;
        Initialize();
    }

    private void Initialize()
    {
        HP = baseStatus.HP;
        Attack = baseStatus.Attack;
        Defend = baseStatus.Defend;
        Move = baseStatus.Move;

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

        Debug.Log($"hp:{HP}, attack:{Attack}, defend:{Defend}");
    }

    /// <summary>
    /// ダメージを反映させる
    /// </summary>
    /// <param name="targetAttack">相手の攻撃力</param>
    /// <returns>死んだかどうか</returns>
    public (int damage, bool isDeath) Damage(int targetAttack)
    {
        // ダメージ計算式
        int damage = targetAttack - Defend / 2;
        if (damage < 0)
        {
            damage = 0;
        }

        HP -= damage;
        Debug.Log($"ダメージをうけた 残り:{HP}");
        if(HP <= 0)
        {
            Debug.Log("死んだぜ");
            return (damage, true);
        }
        return (damage, false);
    }
}
