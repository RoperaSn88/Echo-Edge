using System;

/// <summary>
/// 防御力を表す Value Object。負の値を持てないことを保証する。
/// </summary>
public readonly struct DefenseValue : IEquatable<DefenseValue>
{
    public int Value { get; }

    public DefenseValue(int value)
    {
        if (value < 0) throw new ArgumentOutOfRangeException(nameof(value), "防御力は0以上でなければなりません。");
        Value = value;
    }

    public DefenseValue Add(int amount) => new DefenseValue(Math.Max(0, Value + amount));

    public static implicit operator int(DefenseValue d) => d.Value;
    public static explicit operator DefenseValue(int v) => new DefenseValue(v);

    public bool Equals(DefenseValue other) => Value == other.Value;
    public override bool Equals(object obj) => obj is DefenseValue d && Equals(d);
    public override int GetHashCode() => Value.GetHashCode();
    public static bool operator ==(DefenseValue a, DefenseValue b) => a.Value == b.Value;
    public static bool operator !=(DefenseValue a, DefenseValue b) => a.Value != b.Value;
    public override string ToString() => Value.ToString();
}
