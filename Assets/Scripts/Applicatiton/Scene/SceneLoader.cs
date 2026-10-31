using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using UnityEngine;
using UnityEngine.SceneManagement;

using EchoEdge.Domain.Scene;
using EchoEdge.Infra.Audio;

namespace EchoEdge.App.Scene
{
    public static class SceneLoader
    {
        /// <summary>
        /// <see cref="AdditiveLoadAndWait(int)"/> でロード中〜アンロード待ちのシーンのビルドインデックス。
        /// 非同期ロード中は <c>Scene.isLoaded</c> が false のため、重複ロードの判定に使う。
        /// </summary>
        private static readonly HashSet<int> WaitingSceneIndices = new();

        /// <summary>
        /// 指定シーンがロード済み、またはロード中かどうか。
        /// </summary>
        public static bool IsLoadedOrLoading(GameScene scene)
        {
            var sceneIndex = (int)scene;
            return WaitingSceneIndices.Contains(sceneIndex)
                || SceneManager.GetSceneByBuildIndex(sceneIndex).isLoaded;
        }

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
        /// シーンで使う SE をロードしてからシーンを追加ロードし、ロード完了まで待機します。
        /// </summary>
        public static async UniTask AdditiveLoadAsync(GameScene scene)
        {
            if (!TryGetSceneIndex(scene, out var sceneIndex))
            {
                return;
            }

            await LoadSeAsync(scene);
            await AdditiveLoadAsync(sceneIndex);
        }

        /// <summary>
        /// ビルドインデックスで指定されたシーンを追加ロードし、ロード完了まで待機します。
        /// </summary>
        public static async UniTask AdditiveLoadAsync(int sceneIndex)
        {
            if (sceneIndex < 0 || sceneIndex >= SceneManager.sceneCountInBuildSettings)
            {
                Debug.LogError($"Build Settings にシーンインデックス {sceneIndex} が存在しません");
                return;
            }

            await SceneManager.LoadSceneAsync(sceneIndex, LoadSceneMode.Additive).ToUniTask();
        }

        /// <summary>
        /// シーンで使う SE をロードしてからシーンを追加ロードし、そのシーンがアンロードされるまで待機します。
        /// </summary>
        public static async UniTask AdditiveLoadAndWait(GameScene scene)
        {
            if (!TryGetSceneIndex(scene, out var sceneIndex))
            {
                return;
            }

            await LoadSeAsync(scene);
            await AdditiveLoadAndWait(sceneIndex);
            ReleaseSe(scene);
        }

        /// <summary>
        /// ビルドインデックスで指定されたシーンを追加ロードし、アンロードされるまで待機します。
        /// </summary>
        public static async UniTask AdditiveLoadAndWait(int sceneIndex)
        {
            if (sceneIndex < 0 || sceneIndex >= SceneManager.sceneCountInBuildSettings)
            {
                Debug.LogError($"Build Settings にシーンインデックス {sceneIndex} が存在しません");
                return;
            }

            // 同じシーンが既に開いている（またはロード中の）場合は二重に開かない
            if (!WaitingSceneIndices.Add(sceneIndex))
            {
                Debug.LogWarning($"シーンは既にロード中です: {sceneIndex}");
                return;
            }

            try
            {
                if (SceneManager.GetSceneByBuildIndex(sceneIndex).isLoaded)
                {
                    Debug.LogWarning($"シーンは既にロードされています: {sceneIndex}");
                    return;
                }

                await SceneManager.LoadSceneAsync(sceneIndex, LoadSceneMode.Additive).ToUniTask();
                var loadedScene = SceneManager.GetSceneByBuildIndex(sceneIndex);
                await UniTask.WaitUntil(() => !loadedScene.isLoaded);
            }
            finally
            {
                WaitingSceneIndices.Remove(sceneIndex);
            }
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
                return;
            }

            // アンロード中のシーンのオブジェクトが SE を鳴らせるよう、完了してから解放する
            unloadOperation.completed += _ => ReleaseSe(scene);
        }

        /// <summary>
        /// シーンをアンロードし、アンロード完了まで待機します。
        /// 同じシーンを読み込み直す前に、古いインスタンスを確実に破棄したいときに使います。
        /// </summary>
        public static async UniTask UnloadAsync(GameScene scene)
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
                return;
            }

            await unloadOperation.ToUniTask();
            ReleaseSe(scene);
        }

        private static UniTask LoadSeAsync(GameScene scene)
        {
            if (AudioManager.Instance == null)
            {
                return UniTask.CompletedTask;
            }

            return AudioManager.Instance.LoadSeAsync(scene);
        }

        private static void ReleaseSe(GameScene scene)
        {
            if (AudioManager.Instance == null)
            {
                return;
            }

            AudioManager.Instance.ReleaseSe(scene);
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
}
