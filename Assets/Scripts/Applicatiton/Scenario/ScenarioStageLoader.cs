using Cysharp.Threading.Tasks;
using UnityEngine;
using UnityEngine.SceneManagement;

using EchoEdge.App.Scene;
using EchoEdge.Domain.Preparing;
using EchoEdge.Domain.Scene;

namespace EchoEdge.App.Scenario
{
    /// <summary>
    /// ステージ選択完了時に、選択中のステージに対応するシナリオを読み込んで再生するクラス。
    /// Scenario シーンを追加ロードして <see cref="ScenarioScreen"/> を実行し、
    /// シナリオの再生が終わったらシーンをアンロードする。
    /// </summary>
    public static class ScenarioStageLoader
    {
        private const string ScenarioAddressBeforeFormat = "Assets/Addressables/Scenario/ScenarioData_{0}_before.asset";
        private const string ScenarioAddressAfterFormat = "Assets/Addressables/Scenario/ScenarioData_{0}_after.asset";
        private const string PrologueScenarioAddress = "Assets/Addressables/Scenario/ScenarioData_Prologue.asset";

        /// <summary>
        /// 現在選択されているステージレベルに対応するシナリオを Scenario シーンで再生し、
        /// 再生が終了するまで待機する。
        /// シナリオ再生の要否は呼び出し元（<c>StartText</c>）が
        /// <see cref="StageScenarioPlaybackSettings"/> で判定済みであることを前提とし、
        /// このメソッドは常にシナリオの読み込み・再生を試みる。
        /// </summary>
        /// <returns>
        /// Scenario シーンをロードした場合は true（呼び出し元でのアンロードが必要）。
        /// Scenario シーンのロードに失敗した場合は false。
        /// </returns>
        public static async UniTask<bool> PlayCurrentBeforeStageScenarioAsync()
        {
            var address = string.Format(ScenarioAddressBeforeFormat, StageData.Level);
            var (sceneLoaded, _) = await PlayScenarioAsync(address);
            return sceneLoaded;
        }

        public static async UniTask<bool> PlayCurrentAfterStageScenarioAsync()
        {
            var address = string.Format(ScenarioAddressAfterFormat, StageData.Level);
            var (sceneLoaded, _) = await PlayScenarioAsync(address);
            return sceneLoaded;
        }

        /// <summary>
        /// 初回起動時のプロローグシナリオを Scenario シーンで再生し、再生が終了するまで待機する。
        /// 対応するシナリオデータが存在しない場合は何も表示せずに終了する。
        /// </summary>
        /// <returns>
        /// プロローグシナリオが実際に再生された場合は true。
        /// Addressables のロード失敗・空データ・シーンのロード失敗などで再生できなかった場合は false
        /// （この場合、呼び出し元は「再生済み」として永続化してはならない）。
        /// </returns>
        public static async UniTask<bool> PlayPrologueScenarioAsync()
        {
            var (_, played) = await PlayScenarioAsync(PrologueScenarioAddress);
            return played;
        }

        /// <summary>
        /// 指定した Addressable アドレスのシナリオデータを Scenario シーンで再生し、
        /// 再生が終了するまで待機する。
        /// </summary>
        /// <returns>
        /// <c>sceneLoaded</c>: Scenario シーンがロードされている場合は true（呼び出し元でのアンロードが必要）。
        /// <c>played</c>: 再生可能なシナリオデータを読み込んで実際に再生した場合は true。
        /// </returns>
        private static async UniTask<(bool sceneLoaded, bool played)> PlayScenarioAsync(string scenarioAddress)
        {
            await SceneLoader.AdditiveLoadAsync(GameScene.Scenario);

            // Build Settings にシーンが登録されていない場合など、SceneLoader 側でロードに失敗して
            // 何もしていない可能性があるため、実際にロードされたかどうかを確認してから続行する
            if (!SceneManager.GetSceneByBuildIndex((int)GameScene.Scenario).isLoaded)
            {
                Debug.LogError("Scenario シーンのロードに失敗したため、シナリオの再生をスキップします");
                return (false, false);
            }

            // ScenarioScreen は初期状態で非表示（非アクティブ）のため、非アクティブなオブジェクトも検索対象に含める
            var screen = UnityEngine.Object.FindFirstObjectByType<ScenarioScreen>(FindObjectsInactive.Include);
            if (screen == null)
            {
                Debug.LogError("Scenario シーンに ScenarioScreen が見つかりませんでした");
                return (true, false);
            }

            var hasContent = await screen.Initialize(scenarioAddress);
            if (!hasContent)
            {
                // Addressables のコンテンツビルド漏れなどでシナリオデータが読み込めなかったケース。
                // 画面を表示せずに終了し、呼び出し元が「再生済み」を保存しないようにする。
                Debug.LogWarning($"シナリオデータを読み込めなかったため再生をスキップします (address: {scenarioAddress})");
                return (true, false);
            }

            await screen.ShowAndWaitForFinishAsync();
            return (true, true);
        }
    }
}
