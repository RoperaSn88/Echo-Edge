using Cysharp.Threading.Tasks;
using UnityEngine;

namespace Applicatiton.Scenario
{
    /// <summary>
    /// ステージ選択完了時に、選択中のステージに対応するシナリオを読み込んで再生するクラス。
    /// Scenario シーンを追加ロードして <see cref="ScenarioScreen"/> を実行し、
    /// シナリオの再生が終わったらシーンをアンロードする。
    /// </summary>
    public static class ScenarioStageLoader
    {
        private const string ScenarioAddressFormat = "Assets/Addressables/Scenario/ScenarioData_{0}.asset";

        /// <summary>
        /// 現在選択されているステージレベルに対応するシナリオを Scenario シーンで再生し、
        /// 再生が終了するまで待機する。対応するシナリオデータが存在しない場合は何も表示せずに終了する。
        /// </summary>
        public static async UniTask PlayCurrentStageScenarioAsync()
        {
            await SceneLoader.AdditiveLoadAsync(GameScene.Scenario);

            // ScenarioScreen は初期状態で非表示（非アクティブ）のため、非アクティブなオブジェクトも検索対象に含める
            var screen = UnityEngine.Object.FindFirstObjectByType<ScenarioScreen>(FindObjectsInactive.Include);
            if (screen == null)
            {
                Debug.LogError("Scenario シーンに ScenarioScreen が見つかりませんでした");
                SceneLoader.Unload(GameScene.Scenario);
                return;
            }

            var address = string.Format(ScenarioAddressFormat, StageData.Level);

            try
            {
                await screen.Initialize(address);
                await screen.ShowAndWaitForFinishAsync();
            }
            finally
            {
                SceneLoader.Unload(GameScene.Scenario);
            }
        }
    }
}
