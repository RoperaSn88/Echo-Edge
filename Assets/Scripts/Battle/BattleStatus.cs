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
    private int _level = 1;
    /// <summary>
    /// レベル
    /// </summary>
    public int Level => _level;
    
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

    /// <summary>
    /// ダメージを無効化するか（飛行中など）
    /// </summary>
    [NonSerialized]
    public bool IsInvincible;

    private const int ExperiencePerLevel = 100;

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

    public void SetLevel(int level)
    {
        _level = Mathf.Max(1, level);
    }

    /// <summary>
    /// 経験値を加算する。
    /// </summary>
    public void AddExperience(int experience)
    {
        if (experience < 0)
        {
            throw new ArgumentOutOfRangeException(nameof(experience), "experience must be greater than or equal to 0.");
        }

        _experience += experience;
    }

    /// <summary>
    /// 100EXP ごとにレベルアップし、レベルアップ分の経験値を消費する。
    /// </summary>
    /// <returns>上昇したレベル数</returns>
    public int LevelUp()
    {
        var levelUpCount = 0;
        while (_experience >= ExperiencePerLevel)
        {
            _experience -= ExperiencePerLevel;
            _level++;
            levelUpCount++;
            
            MaxHP += 10; // レベルアップごとに最大HPが10増える
            HP = MaxHP; // レベルアップしたらHPを全回復
            Attack += 5; // レベルアップごとに攻撃力が2増
            Defend += 1; // レベルアップごとに防御力が1増える
        }

        return levelUpCount;
    }

    /// <summary>
    /// 指定した種類のバフが現在かかっているか確認する
    /// </summary>
    public bool HasBuff(BuffKinds kind)
    {
        return _activeBuffs.Any(b => b.buff.GetBuffKinds() == kind);
    }

    /// <summary>
    /// バフを付与し、ターン数つきで管理リストに追加する。
    /// 同じ種類のバフが既に付与されている場合は何もしない（1体につき1種類1つまで）。
    /// </summary>
    /// <param name="buff">付与するバフ</param>
    /// <param name="durationTurns">効果が続くターン数</param>
    public void AddBuff(IBuff buff, int durationTurns)
    {
        if (HasBuff(buff.GetBuffKinds())) return;
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
        // 無敵状態ならダメージを受けない
        if (IsInvincible)
        {
            return (0, false);
        }

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
            return (damage, true);
        }
        return (damage, false);
    }
}
