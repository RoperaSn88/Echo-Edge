using System;
using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using UnityEngine;
using UnityEngine.AddressableAssets;

/// <summary>
/// ウェーブ構築用 CSV の読み込み。
/// CSV フォーマット:
///   1行目: ConditionType,ConditionValue  (このウェーブのクリア条件)
///   2行目: ObjectKind,Height,Width,EnemyKind  (見出し。読み飛ばす)
///   3行目以降: マップ配置データ
/// </summary>
public static class StageLayoutLoader
{
    private const string LegacyCsvAddressFormat = "Assets/Addressables/StageLayout_{0}.csv";

    /// <summary>
    /// StageData.Level に対応する CSV を1ウェーブとして読み込む（後方互換用）。
    /// </summary>
    public static UniTask<WaveLayoutData> GetWaveAsync(int mapHeight, int mapWidth)
    {
        var csvAddress = string.Format(LegacyCsvAddressFormat, StageData.Level);
        return GetWaveAsync(csvAddress, mapHeight, mapWidth);
    }

    /// <summary>
    /// 指定した Addressables アドレスの CSV を1ウェーブとして読み込む。
    /// </summary>
    public static async UniTask<WaveLayoutData> GetWaveAsync(string csvAddress, int mapHeight, int mapWidth)
    {
        var placements = new List<StagePlacementData>();
        var occupied = new HashSet<(int height, int width)>();
        var conditionType = StageClearConditionType.DefeatAllEnemies;
        var conditionValue = 0;

        var csv = await Addressables.LoadAssetAsync<TextAsset>(csvAddress);
        if (csv == null)
        {
            Debug.LogError($"ウェーブ CSV が見つかりません (address: {csvAddress})");
            return new WaveLayoutData(conditionType, conditionValue, placements);
        }

        var lines = csv.text.Split(new[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries);
        if (lines.Length < 1)
        {
            Debug.LogWarning($"{csvAddress} が空です");
            return new WaveLayoutData(conditionType, conditionValue, placements);
        }

        // 1行目: クリア条件 (ConditionType,ConditionValue)
        var conditionCols = lines[0].Split(',');
        if (conditionCols.Length >= 1 && Enum.TryParse(conditionCols[0].Trim(), true, out StageClearConditionType parsedType))
        {
            conditionType = parsedType;
            if (conditionCols.Length >= 2 && int.TryParse(conditionCols[1].Trim(), out int parsedValue))
            {
                conditionValue = parsedValue;
            }
        }
        else
        {
            Debug.LogWarning($"{csvAddress} の1行目のクリア条件が不正です: {lines[0]}。DefeatAllEnemies として扱います。");
        }

        // 2行目はヘッダー行 (ObjectKind,Height,Width,EnemyKind) なので読み飛ばし、3行目以降を配置データとして読む
        for (int i = 2; i < lines.Length; i++)
        {
            var cols = lines[i].Split(',');
            if (cols.Length < 3)
            {
                Debug.LogWarning($"{csvAddress} の {i + 1} 行目の列数が不足しています。最低3列（ObjectKind,Height,Width）が必要です。現在:{cols.Length}列");
                continue;
            }

            if (!Enum.TryParse(cols[0].Trim(), true, out StageObjectKind objectKind))
            {
                Debug.LogWarning($"{csvAddress} の {i + 1} 行目の objectKind が不正です: {cols[0]}。有効値: Wall, Unit");
                continue;
            }

            if (!int.TryParse(cols[1].Trim(), out int height) || !int.TryParse(cols[2].Trim(), out int width))
            {
                Debug.LogWarning($"{csvAddress} の {i + 1} 行目の座標が不正です");
                continue;
            }

            if (height < 0 || height >= mapHeight || width < 0 || width >= mapWidth)
            {
                Debug.LogWarning($"{csvAddress} の {i + 1} 行目が範囲外です。h:{height}, w:{width}");
                continue;
            }

            if (!occupied.Add((height, width)))
            {
                Debug.LogWarning($"{csvAddress} の {i + 1} 行目は重複座標です。h:{height}, w:{width}");
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
                    Debug.LogWarning($"{csvAddress} の {i + 1} 行目の enemyKind 列が不足しています");
                    continue;
                }

                if (!Enum.TryParse(cols[3].Trim(), true, out EnemyKinds enemyKind))
                {
                    Debug.LogWarning($"{csvAddress} の {i + 1} 行目の enemyKind が不正です: {cols[3]}");
                    continue;
                }

                if (enemyKind == EnemyKinds.Invalid)
                {
                    Debug.LogWarning($"{csvAddress} の {i + 1} 行目の enemyKind に Invalid は使用できません");
                    continue;
                }

                placement.enemyKind = enemyKind;
            }

            placements.Add(placement);
        }

        return new WaveLayoutData(conditionType, conditionValue, placements);
    }
}
