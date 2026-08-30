using System;
using Cysharp.Threading.Tasks;
using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.SceneManagement;

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
        private const string PrologueScenarioAddress = "Assets/Addressables/Scenario/ScenarioData_Prologue.asset";
        private const string StageWaveSetAddressFormat = "Assets/Addressables/StageWaves/WaveSet/StageWaveSet_{0}.asset";

        /// <summary>
        /// 現在選択されているステージレベルに対応するシナリオを Scenario シーンで再生し、
        /// 再生が終了するまで待機する。
        /// ステージ設定（<see cref="StageWaveSetData.PlayScenario"/>）でシナリオ再生が無効の場合は、
        /// Scenario シーンをロードせず、そのまま呼び出し元に戻る。
        /// </summary>
        /// <returns>
        /// Scenario シーンをロードした場合は true（呼び出し元でのアンロードが必要）。
        /// シナリオ再生をスキップした場合、または Scenario シーンのロードに失敗した場合は false。
        /// </returns>
        public static async UniTask<bool> PlayCurrentStageScenarioAsync()
        {
            if (!await ShouldPlayCurrentStageScenarioAsync())
            {
                Debug.Log($"ステージ {StageData.Level} はシナリオ再生が無効のため、Scenario シーンをロードせずにメインゲームへ進みます");
                return false;
            }

            var address = string.Format(ScenarioAddressFormat, StageData.Level);
            return await PlayScenarioAsync(address);
        }

        /// <summary>
        /// 初回起動時のプロローグシナリオを Scenario シーンで再生し、再生が終了するまで待機する。
        /// 対応するシナリオデータが存在しない場合は何も表示せずに終了する。
        /// </summary>
        public static async UniTask PlayPrologueScenarioAsync()
        {
            await PlayScenarioAsync(PrologueScenarioAddress);
        }

        /// <summary>
        /// 現在選択されているステージがシナリオ再生の対象かどうかを、ステージ設定から判定する。
        /// ステージ設定が取得できない場合は、従来通りシナリオを再生する（true を返す）。
        /// </summary>
        private static async UniTask<bool> ShouldPlayCurrentStageScenarioAsync()
        {
            var address = string.Format(StageWaveSetAddressFormat, StageData.Level);

            try
            {
                var waveSet = await Addressables.LoadAssetAsync<StageWaveSetData>(address);
                if (waveSet == null)
                {
                    Debug.LogWarning($"ステージ {StageData.Level} の StageWaveSetData が見つからないため、シナリオを再生します (address: {address})");
                    return true;
                }

                return waveSet.PlayScenario;
            }
            catch (Exception e)
            {
                Debug.LogError($"ステージ {StageData.Level} の StageWaveSetData 読み込みに失敗したため、シナリオを再生します: {e}");
                return true;
            }
        }

        /// <summary>
        /// 指定した Addressable アドレスのシナリオデータを Scenario シーンで再生し、
        /// 再生が終了するまで待機する。
        /// </summary>
        /// <returns>Scenario シーンがロードされている場合は true（呼び出し元でのアンロードが必要）。</returns>
        private static async UniTask<bool> PlayScenarioAsync(string scenarioAddress)
        {
            await SceneLoader.AdditiveLoadAsync(GameScene.Scenario);

            // Build Settings にシーンが登録されていない場合など、SceneLoader 側でロードに失敗して
            // 何もしていない可能性があるため、実際にロードされたかどうかを確認してから続行する
            if (!SceneManager.GetSceneByBuildIndex((int)GameScene.Scenario).isLoaded)
            {
                Debug.LogError("Scenario シーンのロードに失敗したため、シナリオの再生をスキップします");
                return false;
            }

            // ScenarioScreen は初期状態で非表示（非アクティブ）のため、非アクティブなオブジェクトも検索対象に含める
            var screen = UnityEngine.Object.FindFirstObjectByType<ScenarioScreen>(FindObjectsInactive.Include);
            if (screen == null)
            {
                Debug.LogError("Scenario シーンに ScenarioScreen が見つかりませんでした");
                return true;
            }

            await screen.Initialize(scenarioAddress);
            await screen.ShowAndWaitForFinishAsync();
            return true;
        }
    }
}
