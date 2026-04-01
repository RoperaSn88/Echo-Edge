using System;
using UnityEngine;

[Serializable]
public class BattleStatus : IDamagable
{
    /// <summary>
    /// 最大HP
    /// </summary>
    [NonSerialized]
    public int MaxHP;
    
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
    
    /// <summary>
    /// 攻撃をいつやるか
    /// </summary>
    public MovePattern MovePattern;
    
    [SerializeField]
    private int _experience;
    /// <summary>
    /// 経験値
    /// </summary>
    public int Experience => _experience;
    
    [SerializeField]
    private int _energy;
    /// <summary>
    /// 落とすエナジーの量
    /// </summary>
    public int Energy => _energy;

    public void Initialize()
    {
        MaxHP = HP;
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
