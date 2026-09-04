using UnityEngine;

namespace EchoEdge.Infra.Preparing
{
    /// <summary>
    /// ステージの進行状況（選択可能な最大ステージ番号）を PlayerPrefs で保存・読み込みするマネージャー。
    /// </summary>
    public static class StageProgressSaveManager
    {
        private const string HighestClearedStageKey = "StageProgress.HighestClearedStage";
        private const int DefaultHighestClearedStage = 1;

        /// <summary>
        /// 進行状況のデータが保存済みか確認する。
        /// </summary>
        public static bool HasHighestClearedStageData()
        {
            return PlayerPrefs.HasKey(HighestClearedStageKey);
        }

        /// <summary>
        /// 選択可能な最大ステージ番号を読み込む。保存データがない場合は初期値（1）を返す。
        /// </summary>
        public static int LoadHighestClearedStage()
        {
            return PlayerPrefs.GetInt(HighestClearedStageKey, DefaultHighestClearedStage);
        }

        /// <summary>
        /// 選択可能な最大ステージ番号を保存する。
        /// </summary>
        public static void SaveHighestClearedStage(int highestClearedStage)
        {
            PlayerPrefs.SetInt(HighestClearedStageKey, highestClearedStage);
            PlayerPrefs.Save();
        }

        public static void DeleteAllSavedData()
        {
            PlayerPrefs.DeleteKey(HighestClearedStageKey);
        }
    }
}
