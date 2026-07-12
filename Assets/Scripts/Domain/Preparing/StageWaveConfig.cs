using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// ステージを構成するウェーブの並び順を定義するスクリプタブルオブジェクト。
/// 配列0番目のウェーブから順に構築し、各ウェーブのクリア条件が満たされるたびに次の要素へ進む。
/// 最後の要素のクリア条件が満たされるとステージクリアとなる。
/// </summary>
[CreateAssetMenu(fileName = "StageWaveConfig", menuName = "EchoEdge/Stage/StageWaveConfig")]
public class StageWaveConfig : ScriptableObject
{
    /// <summary>
    /// ウェーブ順に並んだ、各ウェーブの CSV の Addressables アドレス
    /// </summary>
    [SerializeField]
    private string[] _waveCsvAddresses;

    public IReadOnlyList<string> WaveCsvAddresses => _waveCsvAddresses;
}
