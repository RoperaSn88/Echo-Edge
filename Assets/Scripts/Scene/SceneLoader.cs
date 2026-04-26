using Cysharp.Threading.Tasks;
using UnityEngine;
using UnityEngine.SceneManagement;

public static class SceneLoader
{
    public static void Load(GameScene scene)
    {
        if (!TryGetSceneIndex(scene, out var sceneIndex))
        {
            return;
        }

        SceneManager.LoadScene(sceneIndex);
    }

    public static void AdditiveLoad(GameScene scene)
    {
        if (!TryGetSceneIndex(scene, out var sceneIndex))
        {
            return;
        }

        SceneManager.LoadScene(sceneIndex, LoadSceneMode.Additive);
    }

    /// <summary>
    /// シーンを追加ロードし、そのシーンがアンロードされるまで待機します。
    /// </summary>
    public static async UniTask AdditiveLoadAndWait(GameScene scene)
    {
        if (!TryGetSceneIndex(scene, out var sceneIndex))
        {
            return;
        }

        await SceneManager.LoadSceneAsync(sceneIndex, LoadSceneMode.Additive).ToUniTask();
        var loadedScene = SceneManager.GetSceneByBuildIndex(sceneIndex);
        await UniTask.WaitUntil(() => !loadedScene.isLoaded);
    }

    public static void Unload(GameScene scene)
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
