using Cysharp.Threading.Tasks;
using UnityEngine;
using UnityEngine.AddressableAssets;

/// <summary>
/// ステージを構成するウェーブの読み込みと進行を管理するstaticクラス
/// </summary>
public static class WaveManager
{
    private const string WaveSetAddressFormat = "Assets/Addressables/StageWaves/WaveSet/StageWaveSet_{0}.asset";

    private static TextAsset[] _waveCsvList = System.Array.Empty<TextAsset>();

    /// <summary>
    /// 現在のウェーブのインデックス（0始まり）
    /// </summary>
    public static int CurrentWaveIndex { get; private set; }

    /// <summary>
    /// 次のウェーブが存在するか
    /// </summary>
    public static bool HasNextWave => CurrentWaveIndex < _waveCsvList.Length - 1;

    /// <summary>
    /// 現在のウェーブの配置用CSV
    /// </summary>
    public static TextAsset CurrentWaveCsv =>
        CurrentWaveIndex >= 0 && CurrentWaveIndex < _waveCsvList.Length ? _waveCsvList[CurrentWaveIndex] : null;

    /// <summary>
    /// 現在のステージレベルのウェーブ定義をAddressableから読み込み、ウェーブ0番目から開始する状態にする。
    /// ステージ開始時（StartPhase）に呼び出す。
    /// </summary>
    public static async UniTask LoadStageWavesAsync()
    {
        var address = string.Format(WaveSetAddressFormat, StageData.Level);
        var waveSet = await Addressables.LoadAssetAsync<StageWaveSetData>(address);
        if (waveSet == null || waveSet.WaveCsvList == null || waveSet.WaveCsvList.Length == 0)
        {
            Debug.LogError($"ステージ {StageData.Level} のウェーブ定義が見つかりません (address: {address})");
            _waveCsvList = System.Array.Empty<TextAsset>();
        }
        else
        {
            _waveCsvList = waveSet.WaveCsvList;
        }

        CurrentWaveIndex = 0;
    }

    /// <summary>
    /// プレイヤーの攻撃終了後に呼び出す。ウェーブクリア条件を満たしていて次のウェーブが存在する場合は
    /// 次のウェーブを読み込んでプレイヤーフェーズへ戻し、そうでない場合は敵フェーズへ進む。
    /// </summary>
    public static async UniTask<IPhase> ResolvePhaseAfterAttackAsync()
    {
        if (GameClearManager.GameClearCondition() && HasNextWave)
        {
            CurrentWaveIndex++;
            return NextWavePhase.Instance;
        }

        return EnemyPhase.Instance;
    }
}
