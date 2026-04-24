using UnityEngine;
using UnityEngine.SceneManagement;

public class SceneLoader : MonoBehaviour
{
    public void Load(GameScene scene)
    {
        if (!TryGetSceneIndex(scene, out var sceneIndex))
        {
            return;
        }

        SceneManager.LoadScene(sceneIndex);
    }

    public void AdditiveLoad(GameScene scene)
    {
        if (!TryGetSceneIndex(scene, out var sceneIndex))
        {
            return;
        }

        SceneManager.LoadScene(sceneIndex, LoadSceneMode.Additive);
    }

    public void Unload(GameScene scene)
    {
        if (!TryGetSceneIndex(scene, out var sceneIndex))
        {
            return;
        }

        if (!SceneManager.GetSceneByBuildIndex(sceneIndex).isLoaded)
        {
            Debug.LogWarning($"アンロード対象シーンがロードされていません: {scene}");
            return;
        }

        var unloadOperation = SceneManager.UnloadSceneAsync(sceneIndex);
        if (unloadOperation == null)
        {
            Debug.LogError($"シーンのアンロード開始に失敗しました: {scene}");
        }
    }

    private static bool TryGetSceneIndex(GameScene scene, out int sceneIndex)
    {
        sceneIndex = (int)scene;
        if (sceneIndex < 0 || sceneIndex >= SceneManager.sceneCountInBuildSettings)
        {
            Debug.LogError($"Build Settings にシーンインデックス {sceneIndex} が存在しません: {scene}");
            return false;
        }

        return true;
    }
}
