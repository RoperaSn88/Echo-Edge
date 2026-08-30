using UnityEngine;

/// <summary>
/// 1ステージ分のウェーブ構築用 CSV を順番に保持する ScriptableObject
/// </summary>
[CreateAssetMenu(menuName = "Stage/StageWaveSetData")]
public class StageWaveSetData : ScriptableObject
{
    [SerializeField, Tooltip("ウェーブ順に並んだステージ配置 CSV")]
    private TextAsset[] _waveCsvList;

    [SerializeField, Tooltip("ステージ選択時にこのステージのシナリオを再生するか。オフの場合は Scenario シーンをロードせず、すぐにメインゲームへ遷移する")]
    private bool _playScenario = true;

    /// <summary>
    /// ウェーブ順に並んだステージ配置 CSV の一覧
    /// </summary>
    public TextAsset[] WaveCsvList => _waveCsvList;

    /// <summary>
    /// ステージ選択時にこのステージのシナリオを再生するかどうか。
    /// false の場合はシナリオをロードせず、直接メインゲームへ遷移する。
    /// </summary>
    public bool PlayScenario => _playScenario;
}
