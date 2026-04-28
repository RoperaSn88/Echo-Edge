using UnityEngine;

/// <summary>
/// ステージのレベルを管理するstaticクラス
/// </summary>
public static class StageData
{
    /// <summary>
    /// ステージのレベル
    /// </summary>
    public static int Level { get; private set; } = 1;

    /// <summary>
    /// レベルを1増加させる
    /// </summary>
    public static void IncrementLevel()
    {
        Level++;
    }

    /// <summary>
    /// レベルを1減少させる（最小値は1）
    /// </summary>
    public static void DecrementLevel()
    {
        Level = Mathf.Max(1, Level - 1);
    }
}
