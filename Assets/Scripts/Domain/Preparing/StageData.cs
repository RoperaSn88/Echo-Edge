using UnityEngine;

using EchoEdge.Infra.Preparing;

namespace EchoEdge.Domain.Preparing
{
    /// <summary>
    /// ステージのレベルを管理するstaticクラス
    /// </summary>
    public static class StageData
    {
        /// <summary>
        /// 選択可能なステージの最小値
        /// </summary>
        public const int MinLevel = 1;

        /// <summary>
        /// 用意されているステージの最大値
        /// </summary>
        public const int MaxLevel = 10;

        /// <summary>
        /// ステージのレベル
        /// </summary>
        public static int Level { get; private set; } = MinLevel;

        /// <summary>
        /// 選択可能な最大のステージ番号（＝最高クリアステージ数）。
        /// これより大きいステージは選択できない。初期値は1で、セーブデータの対象。
        /// </summary>
        public static int HighestClearedStage { get; private set; } = StageProgressSaveManager.LoadHighestClearedStage();

        /// <summary>
        /// レベルを1増加させる。<see cref="HighestClearedStage"/> より大きい値へは進められない。
        /// </summary>
        public static void IncrementLevel()
        {
            if (Level < HighestClearedStage) Level++;
        }

        /// <summary>
        /// レベルを1減少させる（最小値は1）
        /// </summary>
        public static void DecrementLevel()
        {
            Level = Mathf.Max(MinLevel, Level - 1);
        }

        /// <summary>
        /// 指定したステージのクリアを記録し、次のステージを選択可能にする。
        /// 既により先のステージまでクリア済みの場合は何もしない。
        /// </summary>
        public static void RegisterStageCleared(int clearedStage)
        {
            var unlockedStage = Mathf.Min(MaxLevel, clearedStage + 1);
            if (unlockedStage <= HighestClearedStage) return;

            HighestClearedStage = unlockedStage;
            StageProgressSaveManager.SaveHighestClearedStage(HighestClearedStage);
        }
    }
}
