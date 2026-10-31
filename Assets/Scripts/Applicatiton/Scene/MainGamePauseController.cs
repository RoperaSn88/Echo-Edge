using System.Threading;
using Cysharp.Threading.Tasks;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;

using EchoEdge.Domain.Scene;
using EchoEdge.Infra.Audio;
using EchoEdge.Presenter.UI;

namespace EchoEdge.App.Scene
{
    public class MainGamePauseController : MonoBehaviour
    {
        public static MainGamePauseController Instance { get; private set; }
        
        private const int OptionSceneBuildIndex = 3;
        private bool _isPauseOpening;

        [SerializeField]
        private PlayerInput _playerInput;
        
        public CanvasGroup _CanvasGroup;

        private void Awake()
        {
            Instance = this;
            _playerInput.actions["Pause"].performed += OpenPause;
        }

        private void OnDestroy()
        {
            // データリセット等で System シーンが再読み込みされると、このコンポーネントは破棄されるが
            // InputActionAsset は使い回されるため、解除しないと古いハンドラが残り Esc 1回で複数回開いてしまう
            if (_playerInput != null && _playerInput.actions != null)
            {
                _playerInput.actions["Pause"].performed -= OpenPause;
            }
        }
        
        public void OpenPause(InputAction.CallbackContext context)
        {
            if (_isPauseOpening || !CanOpenPause())
            {
                return;
            }
            
            OpenPauseAsync(context).Forget();
        }

        /// <summary>
        /// ポーズ（オプション）を開けるか。
        /// PauseActivater は System シーンに常駐していて Esc を常に拾うため、
        /// メインゲーム中以外や、オプション・クレジット・ライセンスが既に開いているとき
        /// （Esc で閉じた同じフレームを含む）は開かない。
        /// </summary>
        private static bool CanOpenPause()
        {
            if (!SceneLoader.IsLoadedOrLoading(GameScene.MainGame))
            {
                return false;
            }

            return !SceneLoader.IsLoadedOrLoading(GameScene.Option)
                && !SceneLoader.IsLoadedOrLoading(GameScene.Credit)
                && !SceneLoader.IsLoadedOrLoading(GameScene.License);
        }

        private async UniTask OpenPauseAsync(InputAction.CallbackContext context)
        {
            _isPauseOpening = true;
            float previousTimeScale = Time.timeScale;
            bool shouldRestoreTimeScale = true;

            try
            {
                if (_CanvasGroup != null)
                {
                    _CanvasGroup.blocksRaycasts = false;
                    _CanvasGroup.interactable = false;
                }

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
                if (_CanvasGroup != null)
                {
                    _CanvasGroup.blocksRaycasts = true;
                    _CanvasGroup.interactable = true;
                }
                
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
