using System;
using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using UnityEngine;
using UnityEngine.AddressableAssets;

/// <summary>
/// ステージを構成する複数のウェーブを、配列の0番目から順に構築・進行させる。
/// StageWaveConfig が用意されているステージはそれに従い、
/// 用意されていないステージは既存の単一 CSV (StageLayout_{Level}.csv) を1ウェーブとして扱う。
/// </summary>
public static class WaveManager
{
    private const string StageWaveConfigAddressFormat = "Assets/Addressables/StageWaveConfig_{0}.asset";
    private const string LegacyCsvAddressFormat = "Assets/Addressables/StageLayout_{0}.csv";

    private static IReadOnlyList<string> _waveCsvAddresses;
    private static int _currentWaveIndex;
    private static bool _isSubscribed;

    /// <summary>
    /// StageData.Level に対応するステージを構築する。1番目のウェーブから配置を行う。
    /// </summary>
    public static async UniTask BuildStageAsync()
    {
        if (!_isSubscribed)
        {
            GameClearManager.OnWaveCleared += OnWaveClearedAsyncVoid;
            _isSubscribed = true;
        }

        GameClearManager.ResetStageProgress();

        _waveCsvAddresses = await ResolveWaveCsvAddressesAsync();
        _currentWaveIndex = 0;

        await LoadWaveAsync(_currentWaveIndex);
    }

    private static async UniTask<IReadOnlyList<string>> ResolveWaveCsvAddressesAsync()
    {
        var configAddress = string.Format(StageWaveConfigAddressFormat, StageData.Level);
        StageWaveConfig config = null;
        try
        {
            config = await Addressables.LoadAssetAsync<StageWaveConfig>(configAddress);
        }
        catch (Exception)
        {
            config = null;
        }

        if (config != null && config.WaveCsvAddresses != null && config.WaveCsvAddresses.Count > 0)
        {
            return config.WaveCsvAddresses;
        }

        // StageWaveConfig が用意されていないステージは、既存の単一 CSV を1ウェーブとして扱う
        return new[] { string.Format(LegacyCsvAddressFormat, StageData.Level) };
    }

    private static async UniTask LoadWaveAsync(int waveIndex)
    {
        var wave = await StageLayoutLoader.GetWaveAsync(
            _waveCsvAddresses[waveIndex], MapManager.Instance.Height, MapManager.Instance.Width);

        var initialEnemyCount = await MapManager.Instance.BuildWave(wave);

        // DefeatAllEnemies はマップ上のユニット数から条件値を算出する。それ以外は CSV の指定値をそのまま使う。
        var conditionValue = wave.ConditionType == StageClearConditionType.DefeatAllEnemies
            ? initialEnemyCount
            : wave.ConditionValue;

        GameClearManager.SetIsLastWave(waveIndex >= _waveCsvAddresses.Count - 1);
        GameClearManager.SetStageClearConditionType(wave.ConditionType);
        GameClearManager.SetConditionValue(conditionValue);
    }

    private static void OnWaveClearedAsyncVoid()
    {
        AdvanceToNextWaveAsync().Forget();
    }

    private static async UniTask AdvanceToNextWaveAsync()
    {
        _currentWaveIndex++;
        if (_currentWaveIndex >= _waveCsvAddresses.Count)
        {
            Debug.LogError("WaveManager: 存在しないウェーブへの進行が要求されました。");
            return;
        }

        await LoadWaveAsync(_currentWaveIndex);
    }
}
