using System;
using System.Collections.Generic;
using EchoEdge.App.Battle;
using UnityEngine;

using EchoEdge.Domain.Battle;
using EchoEdge.Domain.Map;
using EchoEdge.Domain.Phase;
using EchoEdge.Domain.Preparing;

namespace EchoEdge.Infra.Map
{
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
                    Debug.LogWarning($"StageLayout.csv の {i + 1} 行目の objectKind が不正です: {cols[0]}。有効値: Wall, Unit, SlopeWall");
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

                var placement = new StagePlacementData
                {
                    objectKind = objectKind,
                    height = height,
                    width = width,
                    enemyKind = EnemyKinds.Invalid,
                    level = 0
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

                    // レベル列（任意・最終列）。ステージごとの難易度調整に使う。
                    // 未記載・空欄・0 以下・不正な値の場合は 0（等倍）とする（後方互換のため）。
                    if (cols.Length >= 5 && int.TryParse(cols[4].Trim(), out int level) && level > 0)
                    {
                        placement.level = level;
                    }
                }
                
                // 盤面外に立つエネミー（『シーカー』など）はマスを占有しないため、重複座標の確認・予約から外す
                if (!placement.enemyKind.IsOffBoard() && !occupied.Add((height, width)))
                {
                    Debug.LogWarning($"StageLayout.csv の {i + 1} 行目は重複座標です。h:{height}, w:{width}");
                    continue;
                }
                

                if (objectKind == StageObjectKind.SlopeWall)
                {
                    if (cols.Length < 4)
                    {
                        occupied.Remove((height, width));
                        Debug.LogWarning($"StageLayout.csv の {i + 1} 行目の角度列が不足しています");
                        continue;
                    }

                    if (!int.TryParse(cols[3].Trim(), out int angle) || !TryGetSlopeDirection(angle, out var slopeDirection))
                    {
                        occupied.Remove((height, width));
                        Debug.LogWarning($"StageLayout.csv の {i + 1} 行目の角度が不正です: {cols[3]}。有効値: 45, 135, -135, -45");
                        continue;
                    }

                    placement.slopeDirection = slopeDirection;
                }

                placements.Add(placement);
            }

            return placements;
        }

        /// <summary>
        /// CSV の角度を斜面の向きに変換します。
        /// 角度は真上から見て右（+X）を 0° とした反時計回りで、斜面が向いている方向を表します（45 = 右上、135 = 左上、-135 = 左下、-45 = 右下）。
        /// </summary>
        public static bool TryGetSlopeDirection(int angle, out EnergyWallDirection direction)
        {
            switch (angle)
            {
                case 45:
                    direction = EnergyWallDirection.UpRight;
                    return true;
                case 135:
                    direction = EnergyWallDirection.UpLeft;
                    return true;
                case -135:
                    direction = EnergyWallDirection.DownLeft;
                    return true;
                case -45:
                    direction = EnergyWallDirection.DownRight;
                    return true;
                default:
                    direction = default;
                    return false;
            }
        }

        /// <summary>
        /// CSV に書ける斜面の角度の一覧（<see cref="TryGetSlopeDirection"/> が受け付ける値）
        /// </summary>
        public static readonly int[] SlopeAngles = { 45, 135, -135, -45 };

        /// <summary>
        /// 斜面の向きを CSV の角度に変換します（<see cref="TryGetSlopeDirection"/> の逆変換）。
        /// </summary>
        public static int GetSlopeAngle(EnergyWallDirection direction)
        {
            switch (direction)
            {
                case EnergyWallDirection.UpLeft:
                    return 135;
                case EnergyWallDirection.DownLeft:
                    return -135;
                case EnergyWallDirection.DownRight:
                    return -45;
                default:
                    return 45;
            }
        }
    }
}
