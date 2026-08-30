using System;
using Applicatiton.Scenario;
using Cysharp.Threading.Tasks;
using UnityEngine;

namespace Scene
{
    public class SceneLoaderActivator: MonoBehaviour
    {
        private void Start()
        {
            // 大宮祭用にデリートしてから起動
            PlayerSwordParameterSaveManager.DeleteAllSavedData();
            AudioVolumeSaveManager.DeleteAllSavedData();
            StageProgressSaveManager.DeleteAllSavedData();
            PlayerPrefs.DeleteKey(StartPhase.TutorialCompletedKey);
            PlayerPrefs.Save();

            // 初回起動時はプロローグシナリオを再生してから Preparing シーンを起動する。
            // 再生済みの場合は従来どおり Preparing シーンを直接起動する。
            if (PrologueSaveManager.HasPlayedPrologue())
            {
                SceneLoader.AdditiveLoad(GameScene.Preparing);
                return;
            }

            PlayPrologueThenLoadPreparingAsync().Forget();
        }

        /// <summary>
        /// プロローグシナリオを再生し、再生済みとして保存したうえで Preparing シーンを起動する。
        /// 次回起動時には <see cref="PrologueSaveManager.HasPlayedPrologue"/> が true を返すため、
        /// このメソッドは呼び出されなくなる。
        /// プロローグの再生に失敗した場合でも、finally で Preparing シーンの起動だけは必ず行う
        /// （この場合は再生済みとして保存しないため、次回起動時に再度プロローグの再生を試みる）。
        /// </summary>
        private async UniTask PlayPrologueThenLoadPreparingAsync()
        {
            try
            {
                await ScenarioStageLoader.PlayPrologueScenarioAsync();
                PrologueSaveManager.SaveProloguePlayed();
            }
            catch (Exception e)
            {
                Debug.LogError($"プロローグシナリオの再生に失敗しました。Preparing シーンを起動します: {e}");
            }
            finally
            {
                await SceneLoader.AdditiveLoadAsync(GameScene.Preparing);
                SceneLoader.Unload(GameScene.Scenario);
            }
        }
    }
}
