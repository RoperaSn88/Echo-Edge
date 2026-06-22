using System;
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
            PlayerPrefs.DeleteKey(StartPhase.TutorialCompletedKey);
            PlayerPrefs.Save();
            SceneLoader.AdditiveLoad(GameScene.Preparing);
        }
    }
}
