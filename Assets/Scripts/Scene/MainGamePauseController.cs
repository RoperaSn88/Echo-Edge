using System.Threading;
using Cysharp.Threading.Tasks;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;
using UI;

namespace Scene
{
    public class MainGamePauseController : MonoBehaviour
    {
        private const int OptionSceneBuildIndex = 3;
        private bool _isPauseOpening;

        private void Update()
        {
            if (_isPauseOpening)
            {
                return;
            }

            if (Keyboard.current == null || !Keyboard.current.escapeKey.wasPressedThisFrame)
            {
                return;
            }

            if (!SceneManager.GetSceneByBuildIndex((int)GameScene.MainGame).isLoaded)
            {
                return;
            }

            if (SceneManager.GetSceneByBuildIndex((int)GameScene.Option).isLoaded)
            {
                return;
            }

            OpenPauseAsync().Forget();
        }

        private async UniTaskVoid OpenPauseAsync()
        {
            _isPauseOpening = true;
            float previousTimeScale = Time.timeScale;
            bool shouldRestoreTimeScale = true;

            try
            {
                Time.timeScale = 0f;
                OptionSceneController.CanRetire = true;
                await SceneLoader.AdditiveLoadAndWait(OptionSceneBuildIndex);

                if (OptionSceneController.LastResult != OptionResult.Retire)
                {
                    return;
                }

                shouldRestoreTimeScale = false;
                var t1 = UIPresenter.Instance.FadeOutAsync(0.5f);
                var t2 = GameClearRewardPresenter.Instance.CloseAsync();
                var t3 = AudioManager.Instance != null
                    ? AudioManager.Instance.FadeBGMAsync(0.5f, CancellationToken.None)
                    : UniTask.CompletedTask;
                var t4 = AudioManager.Instance != null
                    ? AudioManager.Instance.FadeAddedBGMAsync(0.5f, CancellationToken.None)
                    : UniTask.CompletedTask;

                await UniTask.WhenAll(t1, t2, t3, t4);
                Time.timeScale = 1.0f;
                await SceneLoader.AdditiveLoadAsync(GameScene.Preparing);
                SceneLoader.Unload(GameScene.MainGame);
            }
            finally
            {
                OptionSceneController.CanRetire = false;
                if (shouldRestoreTimeScale)
                {
                    Time.timeScale = previousTimeScale;
                }

                _isPauseOpening = false;
            }
        }
    }
}
