using System;
using UnityEngine;

/// <summary>
/// マップ上のグリッド座標を表す Value Object。
/// </summary>
[Serializable]
public readonly struct UnitPosition : IEquatable<UnitPosition>
{
    public int Height { get; }
    public int Width { get; }

    public UnitPosition(int height, int width)
    {
        Height = height;
        Width = width;
    }

    /// <summary>マップ左端(width=0)にいるか</summary>
    public bool IsLeftmost => Width <= 0;

    public bool Equals(UnitPosition other) => Height == other.Height && Width == other.Width;
    public override bool Equals(object obj) => obj is UnitPosition p && Equals(p);
    public override int GetHashCode() => HashCode.Combine(Height, Width);
    public static bool operator ==(UnitPosition a, UnitPosition b) => a.Equals(b);
    public static bool operator !=(UnitPosition a, UnitPosition b) => !a.Equals(b);
    public override string ToString() => $"({Height}, {Width})";
}
