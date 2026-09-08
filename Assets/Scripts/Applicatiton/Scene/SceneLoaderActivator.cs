using System;
using Cysharp.Threading.Tasks;
using UnityEngine;

using EchoEdge.App.Scenario;
using EchoEdge.Domain.Phase;
using EchoEdge.Domain.Scene;
using EchoEdge.Infra.Audio;
using EchoEdge.Infra.Battle;
using EchoEdge.Infra.Preparing;
using EchoEdge.Infra.Tutorial;

namespace EchoEdge.App.Scene
{
    public class SceneLoaderActivator: MonoBehaviour
    {
        private void Start()
        {
            // 大宮祭用にデリートしてから起動
            PlayerSwordParameterSaveManager.DeleteAllSavedData();
            AudioVolumeSaveManager.DeleteAllSavedData();
            StageProgressSaveManager.DeleteAllSavedData();
            TutorialSaveManager.DeleteAllSavedData();
            PlayerPrefs.Save();
            
            SceneLoader.AdditiveLoadAsync(GameScene.Preparing).Forget();
        }
    }
}
