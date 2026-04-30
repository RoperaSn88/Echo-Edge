using System;
using System.Collections.Generic;
using System.Linq;
using Cysharp.Threading.Tasks;
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
    public byte Move;
    
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
    /// 落とすエナジーの量, プレイヤーの所持エナジー
    /// </summary>
    public int Energy => _energy;

    /// <summary>
    /// 現在かかっているバフのリスト (バフ, 残りターン数)
    /// </summary>
    [NonSerialized]
    private List<(IBuff buff, int remainingTurns)> _activeBuffs = new();

    public void Initialize()
    {
        MaxHP = HP;
        _activeBuffs = new List<(IBuff, int)>();
    }

    /// <summary>
    /// ステータスを更新する
    /// </summary>
    public void SetStatus(int hp, int attack, int defend, byte move, MovePattern movePattern, int experience, int energy)
    {
        HP = hp;
        MaxHP = hp;
        Attack = attack;
        Defend = defend;
        Move = move;
        MovePattern = movePattern;
        _experience = experience;
        _energy = energy;
    }

    /// <summary>
    /// 指定した種類のバフが現在かかっているか確認する
    /// </summary>
    public bool HasBuff(BuffKinds kind)
    {
        return _activeBuffs.Any(b => b.buff.GetBuffKinds() == kind);
    }

    /// <summary>
    /// バフを付与し、ターン数つきで管理リストに追加する
    /// </summary>
    /// <param name="buff">付与するバフ</param>
    /// <param name="durationTurns">効果が続くターン数</param>
    public void AddBuff(IBuff buff, int durationTurns)
    {
        buff.Buff(this);
        _activeBuffs.Add((buff, durationTurns));
    }

    /// <summary>
    /// EnemyPhase開始時に呼び出し、バフの残りターンを減らして期限切れのバフを消す
    /// </summary>
    public void TickBuffs()
    {
        for (int i = _activeBuffs.Count - 1; i >= 0; i--)
        {
            var (buff, remaining) = _activeBuffs[i];
            remaining--;
            if (remaining <= 0)
            {
                buff.RemoveBuff(this);
                _activeBuffs.RemoveAt(i);
            }
            else
            {
                _activeBuffs[i] = (buff, remaining);
            }
        }
    }

    /// <summary>
    /// ダメージを反映させる
    /// </summary>
    /// <param name="targetAttack">相手の攻撃力</param>
    /// <returns>死んだかどうか</returns>
    public async UniTask<(int damage, bool isDeath)> Damage(int targetAttack)
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
