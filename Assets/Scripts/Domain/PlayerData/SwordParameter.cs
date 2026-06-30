using System;

/// <summary>
/// 剣強化パラメーターを表す Value Object。
/// イミュータブルであり、変更には With* メソッドで新しいインスタンスを生成する。
/// </summary>
public readonly struct SwordParameter
{
    public int HP { get; }
    public int Attack { get; }
    public byte ReflectCount { get; }

    public SwordParameter(int hp, int attack, byte reflectCount)
    {
        if (hp < 0) throw new ArgumentOutOfRangeException(nameof(hp), "HPボーナスは0以上でなければなりません。");
        if (attack < 0) throw new ArgumentOutOfRangeException(nameof(attack), "攻撃力ボーナスは0以上でなければなりません。");
        HP = hp;
        Attack = attack;
        ReflectCount = reflectCount;
    }

    public SwordParameter WithHP(int hp) => new SwordParameter(hp, Attack, ReflectCount);
    public SwordParameter WithAttack(int attack) => new SwordParameter(HP, attack, ReflectCount);
    public SwordParameter WithReflectCount(byte reflectCount) => new SwordParameter(HP, Attack, reflectCount);

    public override string ToString() => $"HP+{HP} ATK+{Attack} Reflect:{ReflectCount}";
}
