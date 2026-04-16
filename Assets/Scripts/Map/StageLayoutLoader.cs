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

    public static async UniTask<IReadOnlyList<StagePlacementData>> GetPlacementsAsync(int mapHeight, int mapWidth)
    {
        var placements = new List<StagePlacementData>();
        var occupied = new HashSet<(int height, int width)>();

        var csv = await Addressables.LoadAssetAsync<TextAsset>(CsvPath);
        if (csv == null)
        {
            Debug.LogError("StageLayout.csv が見つかりません");
            return placements;
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

            if (height < 0 || height >= mapHeight || width < 0 || width >= mapWidth)
            {
                Debug.LogWarning($"StageLayout.csv の {i + 1} 行目が範囲外です。h:{height}, w:{width}");
                continue;
            }

            if (!occupied.Add((height, width)))
            {
                Debug.LogWarning($"StageLayout.csv の {i + 1} 行目は重複座標です。h:{height}, w:{width}");
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
                if (cols.Length < 4)
                {
                    Debug.LogWarning($"StageLayout.csv の {i + 1} 行目の enemyKind 列が不足しています");
                    continue;
                }

                if (!Enum.TryParse(cols[3].Trim(), true, out EnemyKinds enemyKind))
                {
                    Debug.LogWarning($"StageLayout.csv の {i + 1} 行目の enemyKind が不正です: {cols[3]}");
                    continue;
                }

                if (enemyKind == EnemyKinds.Invalid)
                {
                    Debug.LogWarning($"StageLayout.csv の {i + 1} 行目の enemyKind に Invalid は使用できません");
                    continue;
                }

                placement.enemyKind = enemyKind;
            }

            placements.Add(placement);
        }

        return placements;
    }
}
