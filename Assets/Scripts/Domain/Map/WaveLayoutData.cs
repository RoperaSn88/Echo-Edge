using System.Collections.Generic;

/// <summary>
/// ステージ構築用 CSV 1ファイル分（=1ウェーブ分）のデータ。
/// クリア条件とマップ配置をまとめて保持する。
/// </summary>
public sealed class WaveLayoutData
{
    /// <summary>このウェーブのクリア条件タイプ</summary>
    public StageClearConditionType ConditionType { get; }

    /// <summary>
    /// クリア条件に使用する値。
    /// DefeatAllEnemies の場合はマップ上のユニット数から算出するため無視される。
    /// </summary>
    public int ConditionValue { get; }

    /// <summary>マップ配置データ</summary>
    public IReadOnlyList<StagePlacementData> Placements { get; }

    public WaveLayoutData(StageClearConditionType conditionType, int conditionValue, IReadOnlyList<StagePlacementData> placements)
    {
        ConditionType = conditionType;
        ConditionValue = conditionValue;
        Placements = placements;
    }
}
