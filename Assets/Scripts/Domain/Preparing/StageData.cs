using Cysharp.Threading.Tasks;
using UnityEngine;

using EchoEdge.Infra.Map;
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
        /// Addressablesからステージ数を取得できなかった場合に使う、用意されているステージ数の既定値
        /// </summary>
        private const int DefaultMaxLevel = 6;

        /// <summary>
        /// 用意されているステージの最大値。
        /// 初期値は <see cref="DefaultMaxLevel"/> で、<see cref="InitializeMaxLevelAsync"/> を
        /// 呼び出すと Addressables に登録されている WaveSet の数に更新される。
        /// </summary>
        public static int MaxLevel { get; private set; } = DefaultMaxLevel;

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
        /// Addressablesに登録されているWaveSetの数を数え、<see cref="MaxLevel"/> に反映する。
        /// ゲーム起動時（初回ロード時）に1度呼び出すこと。
        /// 1件も見つからない場合は <see cref="DefaultMaxLevel"/> のまま変更しない。
        /// </summary>
        public static async UniTask InitializeMaxLevelAsync()
        {
            var count = await StageWaveSetAvailabilityLoader.CountRegisteredStagesAsync();
            if (count > 0)
            {
                MaxLevel = count;
            }
            else
            {
                Debug.LogWarning("Addressables から WaveSet が1件も見つからなかったため、MaxLevel は既定値のままにします。");
            }
        }

        /// <summary>
        /// メモリ上の状態をセーブデータから読み直す。
        /// <para>
        /// <see cref="Level"/> は最小値に戻し、<see cref="HighestClearedStage"/> は
        /// 現在のセーブデータ（削除済みなら初期値）から再取得する。
        /// セーブデータを削除する「設定リセット」から呼ぶこと。static クラスのため
        /// シーンを再読み込みしても自動では初期化されない。
        /// </para>
        /// </summary>
        public static void ResetToDefault()
        {
            Level = MinLevel;
            HighestClearedStage = StageProgressSaveManager.LoadHighestClearedStage();
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
