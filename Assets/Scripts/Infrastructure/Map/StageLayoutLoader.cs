using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// ステージ構築用 CSV の読み込み
/// </summary>
public static class StageLayoutLoader
{
    /// <summary>
    /// CSV の1行目（クリア条件行）と2行目（ヘッダー行）を除いた、配置データが始まる行番号
    /// </summary>
    private const int PlacementStartLine = 2;

    /// <summary>
    /// CSV の1行目から、ステージクリア条件（種類と値）を読み取ります。
    /// 1列目が条件種別ID、2列目が条件に必要な値です（種類によっては未使用）。
    /// 形式が不正な場合は DefeatAllEnemies（値0）を返します。
    /// </summary>
    public static StageClearConditionData GetClearCondition(TextAsset csv)
    {
        var fallback = new StageClearConditionData
        {
            conditionType = StageClearConditionType.DefeatAllEnemies,
            conditionValue = 0
        };

        if (csv == null)
        {
            Debug.LogError($"ステージ {StageData.Level} のウェーブ {WaveManager.CurrentWaveIndex} の CSV が見つかりません");
            return fallback;
        }

        var lines = csv.text.Split(new[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries);
        if (lines.Length == 0)
        {
            Debug.LogWarning("StageLayout.csv の1行目（クリア条件）が見つかりません");
            return fallback;
        }

        var cols = lines[0].Split(',');
        if (cols.Length < 1 || !int.TryParse(cols[0].Trim(), out var typeId) ||
            !Enum.IsDefined(typeof(StageClearConditionType), typeId))
        {
            Debug.LogWarning($"StageLayout.csv の1行目のクリア条件種別が不正です: {lines[0]}");
            return fallback;
        }

        var conditionValue = 0;
        if (cols.Length >= 2)
        {
            int.TryParse(cols[1].Trim(), out conditionValue);
        }

        return new StageClearConditionData
        {
            conditionType = (StageClearConditionType)typeId,
            conditionValue = conditionValue
        };
    }

    public static IReadOnlyList<StagePlacementData> GetPlacements(TextAsset csv, int mapHeight, int mapWidth)
    {
        var placements = new List<StagePlacementData>();
        var occupied = new HashSet<(int height, int width)>();

        if (csv == null)
        {
            Debug.LogError($"ステージ {StageData.Level} のウェーブ {WaveManager.CurrentWaveIndex} の CSV が見つかりません");
            return placements;
        }

        var lines = csv.text.Split(new[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries);
        for (int i = PlacementStartLine; i < lines.Length; i++)
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
