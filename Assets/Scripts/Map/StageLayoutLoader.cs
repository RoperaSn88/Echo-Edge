using System;
using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using UnityEngine;
using UnityEngine.AddressableAssets;

/// <summary>
/// ステージ構築用 CSV の読み込み
/// </summary>
public static class StageLayoutLoader
{
    private const string CsvPath = "Assets/Addressables/StageLayout.csv";

    private static List<StagePlacementData> _cache;

    public static async UniTask<IReadOnlyList<StagePlacementData>> GetPlacementsAsync()
    {
        if (_cache != null) return _cache;

        _cache = new List<StagePlacementData>();

        var csv = await Addressables.LoadAssetAsync<TextAsset>(CsvPath);
        if (csv == null)
        {
            Debug.LogError("StageLayout.csv が見つかりません");
            return _cache;
        }

        var lines = csv.text.Split(new[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries);
        for (int i = 1; i < lines.Length; i++)
        {
            var cols = lines[i].Split(',');
            if (cols.Length < 3)
            {
                Debug.LogWarning($"StageLayout.csv の {i + 1} 行目の列数が不足しています。最低3列（ObjectKind,Height,Width）が必要です。現在:{cols.Length}列");
                continue;
            }

            if (!Enum.TryParse(cols[0].Trim(), true, out StageObjectKind objectKind))
            {
                Debug.LogWarning($"StageLayout.csv の {i + 1} 行目の objectKind が不正です: {cols[0]}。有効値: Wall, Unit");
                continue;
            }

            if (!int.TryParse(cols[1].Trim(), out int height) || !int.TryParse(cols[2].Trim(), out int width))
            {
                Debug.LogWarning($"StageLayout.csv の {i + 1} 行目の座標が不正です");
                continue;
            }

            var placement = new StagePlacementData
            {
                objectKind = objectKind,
                height = height,
                width = width,
                enemyKind = EnemyKinds.Invalid
            };

            if (objectKind == StageObjectKind.Unit)
            {
                if (cols.Length < 4 || !Enum.TryParse(cols[3].Trim(), true, out EnemyKinds enemyKind))
                {
                    Debug.LogWarning($"StageLayout.csv の {i + 1} 行目の enemyKind が不正です");
                    continue;
                }
                placement.enemyKind = enemyKind;
            }

            _cache.Add(placement);
        }

        return _cache;
    }
}
