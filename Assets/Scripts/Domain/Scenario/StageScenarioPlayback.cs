using UnityEngine;

namespace EchoEdge.Domain.Scenario
{
    /// <summary>
    /// ステージごとのシナリオ再生要否（<see cref="StageScenarioPlaybackSettings"/>）への
    /// 静的アクセスを提供する。
    ///
    /// 設定アセットは Preparing シーンの SelectManager が保持しているが、
    /// クリア後シナリオの判定を行う GameClearManager が動くころには Preparing シーンは
    /// アンロード済みで SelectManager のインスタンスは破棄されている。
    /// SelectManager 起動時にここへ登録しておくことで、シーンやインスタンスに依存せず
    /// どこからでも再生要否を判定できるようにする。
    /// </summary>
    public static class StageScenarioPlayback
    {
        private static StageScenarioPlaybackSettings _settings;

        /// <summary>
        /// シナリオ再生要否設定を登録する。SelectManager の Awake から呼ぶ想定。
        /// </summary>
        public static void RegisterSettings(StageScenarioPlaybackSettings settings)
        {
            _settings = settings;
        }

        /// <summary>
        /// 指定したステージ番号でシナリオを再生するかどうかを返す。
        /// 設定が未登録の場合は、従来どおりシナリオを再生する（true を返す）。
        /// </summary>
        /// <param name="stageLevel">1 始まりのステージ番号（StageData.Level）</param>
        public static bool ShouldPlayScenario(int stageLevel)
        {
            if (_settings == null)
            {
                Debug.LogWarning(
                    $"{nameof(StageScenarioPlaybackSettings)} が未登録のため、シナリオを再生します");
                return true;
            }

            return _settings.ShouldPlayScenario(stageLevel);
        }
    }
}
