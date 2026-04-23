using UnityEngine;
using UnityEngine.SceneManagement;

public class SceneLoader : MonoBehaviour
{
    public void Load(GameScene scene)
    {
        var sceneIndex = (int)scene;
        if (sceneIndex < 0 || sceneIndex >= SceneManager.sceneCountInBuildSettings)
        {
            Debug.LogError($"Build Settings にシーンインデックス {sceneIndex} が存在しません: {scene}");
            return;
        }

        SceneManager.LoadScene(sceneIndex);
    }
}
