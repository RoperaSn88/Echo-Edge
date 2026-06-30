using System;

/// <summary>
/// 攻撃力を表す Value Object。負の値を持てないことを保証する。
/// </summary>
public readonly struct AttackPower : IEquatable<AttackPower>
{
    public int Value { get; }

    public AttackPower(int value)
    {
        if (value < 0) throw new ArgumentOutOfRangeException(nameof(value), "攻撃力は0以上でなければなりません。");
        Value = value;
    }

    public AttackPower Add(int amount) => new AttackPower(Math.Max(0, Value + amount));

    public static implicit operator int(AttackPower a) => a.Value;
    public static explicit operator AttackPower(int v) => new AttackPower(v);

    public bool Equals(AttackPower other) => Value == other.Value;
    public override bool Equals(object obj) => obj is AttackPower a && Equals(a);
    public override int GetHashCode() => Value.GetHashCode();
    public static bool operator ==(AttackPower a, AttackPower b) => a.Value == b.Value;
    public static bool operator !=(AttackPower a, AttackPower b) => a.Value != b.Value;
    public override string ToString() => Value.ToString();
}
