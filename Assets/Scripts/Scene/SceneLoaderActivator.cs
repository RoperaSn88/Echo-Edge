using System;
using UnityEngine;

namespace Scene
{
    public class SceneLoaderActivator: MonoBehaviour
    {
        private void Start()
        {
            SceneLoader.AdditiveLoad(GameScene.Preparing);
        }
    }
}