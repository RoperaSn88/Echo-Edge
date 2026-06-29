using System;

/// <summary>
/// HP を表す Value Object。現在値と最大値をひとまとめに管理し、負にならないことを保証する。
/// </summary>
public readonly struct HitPoint : IEquatable<HitPoint>
{
    public int Current { get; }
    public int Max { get; }

    public bool IsDead => Current <= 0;
    public bool IsFullHealth => Current >= Max;

    public HitPoint(int current, int max)
    {
        if (max < 0) throw new ArgumentOutOfRangeException(nameof(max), "最大HPは0以上でなければなりません。");
        Max = max;
        Current = Math.Clamp(current, 0, max);
    }

    public HitPoint TakeDamage(int damage)
    {
        int actualDamage = Math.Max(0, damage);
        return new HitPoint(Current - actualDamage, Max);
    }

    public HitPoint Restore(int amount) => new HitPoint(Current + amount, Max);

    public HitPoint ExpandMax(int amount) => new HitPoint(Current, Max + amount);

    public HitPoint FullRestore() => new HitPoint(Max, Max);

    public bool Equals(HitPoint other) => Current == other.Current && Max == other.Max;
    public override bool Equals(object obj) => obj is HitPoint h && Equals(h);
    public override int GetHashCode() => HashCode.Combine(Current, Max);
    public static bool operator ==(HitPoint a, HitPoint b) => a.Equals(b);
    public static bool operator !=(HitPoint a, HitPoint b) => !a.Equals(b);
    public override string ToString() => $"{Current}/{Max}";
}
