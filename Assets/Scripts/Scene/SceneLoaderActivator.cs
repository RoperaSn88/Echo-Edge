using System;
using UnityEngine;

namespace Scene
{
    public class SceneLoaderActivator: MonoBehaviour
    {
        private void Start()
        {
            // 大宮祭用にデリートしてから起動
            PlayerPrefs.DeleteAll();
            SceneLoader.AdditiveLoad(GameScene.Preparing);
        }
    }
}