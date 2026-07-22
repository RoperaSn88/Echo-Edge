using UnityEngine;

/// <summary>
/// 1ステージ分のウェーブ構築用 CSV を順番に保持する ScriptableObject
/// </summary>
[CreateAssetMenu(menuName = "Stage/StageWaveSetData")]
public class StageWaveSetData : ScriptableObject
{
    [SerializeField, Tooltip("ウェーブ順に並んだステージ配置 CSV")]
    private TextAsset[] _waveCsvList;

    /// <summary>
    /// ウェーブ順に並んだステージ配置 CSV の一覧
    /// </summary>
    public TextAsset[] WaveCsvList => _waveCsvList;
}
