using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// ステージごとに、ステージ選択確定時のシナリオ再生要否を保持する ScriptableObject。
/// リストの添字は「ステージ番号 - 1」に対応する（要素 0 ＝ ステージ 1）。
/// 要素が true のステージは Scenario シーンをロードしてシナリオを再生し、
/// false のステージは Scenario シーンをロードせず直接メインゲームへ遷移する。
/// </summary>
[CreateAssetMenu(menuName = "Stage/StageScenarioPlaybackSettings")]
public class StageScenarioPlaybackSettings : ScriptableObject
{
    [SerializeField]
    [Tooltip("ステージ番号順（要素0＝ステージ1）に並んだシナリオ再生フラグ。オンでシナリオを再生する")]
    private List<bool> _playScenarioPerStage = new();

    /// <summary>
    /// 指定したステージ番号でシナリオを再生するかどうかを返す。
    /// リストに対応する要素が存在しない場合は、従来どおりシナリオを再生する（true を返す）。
    /// </summary>
    /// <param name="stageLevel">1 始まりのステージ番号（<see cref="StageData.Level"/>）</param>
    public bool ShouldPlayScenario(int stageLevel)
    {
        var index = stageLevel - 1;
        if (index < 0 || index >= _playScenarioPerStage.Count)
        {
            Debug.LogWarning(
                $"ステージ {stageLevel} のシナリオ再生設定が {nameof(StageScenarioPlaybackSettings)} に存在しないため、シナリオを再生します");
            return true;
        }

        return _playScenarioPerStage[index];
    }
}
