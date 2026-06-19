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

        [SerializeField]
        private PlayerInput playerInput;

        private void Awake()
        {
            playerInput.actions["Pause"].performed += OpenPause;
        }
        
        public void OpenPause(InputAction.CallbackContext context)
        {
            if (_isPauseOpening)
            {
                return;
            }
            
            OpenPauseAsync(context).Forget();
        }

        private async UniTask OpenPauseAsync(InputAction.CallbackContext context)
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
