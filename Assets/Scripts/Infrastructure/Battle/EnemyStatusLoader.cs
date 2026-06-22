using System;
using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using UnityEngine;
using UnityEngine.AddressableAssets;

/// <summary>
/// EnemyInfo.csv からエネミーのステータスを読み込むローダー
/// </summary>
public static class EnemyStatusLoader
{
    private const string CsvPath = "Assets/Addressables/EnemyInfo.csv";

    // CSV を初回読み込み時にキャッシュする (ID → 各列の値)
    private static Dictionary<int, string[]> _cache;

    private static async UniTask<Dictionary<int, string[]>> GetCacheAsync()
    {
        if (_cache != null) return _cache;

        _cache = new Dictionary<int, string[]>();

        var csv = await Addressables.LoadAssetAsync<TextAsset>(CsvPath);
        if (csv == null)
        {
            Debug.LogError("EnemyInfo.csv が見つかりません");
            return _cache;
        }

        // Windows (\r\n) と Unix (\n) の両改行に対応
        var lines = csv.text.Split(new[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries);

        // 1行目はヘッダーなのでスキップ
        for (int i = 1; i < lines.Length; i++)
        {
            var cols = lines[i].Split(',');
            if (cols.Length < 8) continue;

            if (!int.TryParse(cols[0].Trim(), out int rowId)) continue;

            _cache[rowId] = cols;
        }

        return _cache;
    }

    /// <summary>
    /// 指定した ID に対応するパラメータを EnemyInfo.csv から読み取り、BattleStatus に反映する
    /// </summary>
    /// <param name="id">読み取る行の ID</param>
    /// <param name="status">更新対象の BattleStatus</param>
    /// <returns>読み取りに成功した場合は true</returns>
    public static async UniTask<bool> TryLoad(int id, BattleStatus status)
    {
        var cache = await GetCacheAsync();

        if (!cache.TryGetValue(id, out var cols))
        {
            Debug.LogWarning($"ID {id} のエネミーが EnemyInfo.csv に見つかりません");
            return false;
        }

        try
        {
            int hp          = int.Parse(cols[1].Trim());
            int attack      = int.Parse(cols[2].Trim());
            int defend      = int.Parse(cols[3].Trim());
            int parsedMove  = int.Parse(cols[4].Trim());
            byte move       = (byte)Mathf.Clamp(parsedMove, byte.MinValue, byte.MaxValue);
            var movePattern = (MovePattern)Enum.Parse(typeof(MovePattern), cols[5].Trim());
            int experience  = int.Parse(cols[6].Trim());
            int energy      = int.Parse(cols[7].Trim());

            status.SetStatus(hp, attack, defend, move, movePattern, experience, energy);
            return true;
        }
        catch (Exception e)
        {
            Debug.LogError($"EnemyInfo.csv の ID {id} の行を解析できませんでした: {e.Message}");
            return false;
        }
    }
}
