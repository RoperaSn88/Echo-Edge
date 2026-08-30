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
            
            SceneLoader.AdditiveLoadAsync(GameScene.Preparing).Forget();
        }
    }
}
