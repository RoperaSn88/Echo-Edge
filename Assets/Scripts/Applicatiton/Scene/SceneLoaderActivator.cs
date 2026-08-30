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
        /// </summary>
        private async UniTask PlayPrologueThenLoadPreparingAsync()
        {
            await ScenarioStageLoader.PlayPrologueScenarioAsync();
            PrologueSaveManager.SaveProloguePlayed();

            await SceneLoader.AdditiveLoadAsync(GameScene.Preparing);
            SceneLoader.Unload(GameScene.Scenario);
        }
    }
}
