using Cysharp.Threading.Tasks;
using UnityEngine;

using EchoEdge.Domain.Scene;

namespace EchoEdge.App.Scene
{
    public class SceneLoaderActivator: MonoBehaviour
    {
        private void Start()
        {
            SceneLoader.AdditiveLoadAsync(GameScene.Preparing).Forget();
        }
    }
}
