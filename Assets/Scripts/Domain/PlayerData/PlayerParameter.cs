using System;

/// <summary>
/// プレイヤーの基本パラメーターを表す Value Object。
/// イミュータブルであり、変更には With* メソッドで新しいインスタンスを生成する。
/// </summary>
public readonly struct PlayerParameter
{
    public int HP { get; }
    public int Attack { get; }
    public int Defend { get; }
    public int Experience { get; }
    public int Level { get; }

    public PlayerParameter(int hp, int attack, int defend, int experience = 0, int level = 1)
    {
        if (hp < 0) throw new ArgumentOutOfRangeException(nameof(hp), "HPは0以上でなければなりません。");
        if (attack < 0) throw new ArgumentOutOfRangeException(nameof(attack), "攻撃力は0以上でなければなりません。");
        if (defend < 0) throw new ArgumentOutOfRangeException(nameof(defend), "防御力は0以上でなければなりません。");
        HP = hp;
        Attack = attack;
        Defend = defend;
        Experience = experience;
        Level = Math.Max(1, level);
    }

    public PlayerParameter WithHP(int hp) => new PlayerParameter(hp, Attack, Defend, Experience, Level);
    public PlayerParameter WithAttack(int attack) => new PlayerParameter(HP, attack, Defend, Experience, Level);
    public PlayerParameter WithDefend(int defend) => new PlayerParameter(HP, Attack, defend, Experience, Level);
    public PlayerParameter WithProgress(int experience, int level) => new PlayerParameter(HP, Attack, Defend, experience, level);

    public override string ToString() => $"Lv{Level} HP:{HP} ATK:{Attack} DEF:{Defend} EXP:{Experience}";
}
