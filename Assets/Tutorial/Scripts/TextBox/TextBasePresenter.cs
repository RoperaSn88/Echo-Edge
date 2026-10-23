using System;
using System.Threading;
using CommonUI.Tutorial.Models;
using CommonUI.Tutorial.Views;
using UnityEngine;
using UnityEngine.UI;
using Cysharp.Threading.Tasks;
using EchoEdge.Infra.Tutorial;
using EchoEdge.Utils;
using UnityEngine.AddressableAssets;

namespace CommonUI.Tutorial
{
    public class TextBasePresenter : MonoBehaviour
    {
        [SerializeField, Tooltip("プレーンのテキストボックス")]
        private TextBoxPresenter _planeTextBox;

        [SerializeField, Tooltip("キャラ付きテキストボックス")]
        private TextBoxPresenter _charaTextBox;

        [SerializeField, Tooltip("キャラクターのスプライト")]
        private Image _charaImage;

        [SerializeField, Tooltip("スキップボタン")]
        private SkipButton _skipButton;

        [SerializeField, Tooltip("テキストボックスのマスターデータ")]
        private TextBoxMasterData _textData;

        /// <summary>
        /// 現在表示中のテキストボックス.
        /// </summary>
        private TextBoxPresenter _currentTextBox;

        private CancellationTokenSource _cts = new();
        
        private static MouseClick _mouseClick;
        public static MouseClick MouseClick => _mouseClick;

        /// <summary>
        /// チュートリアル再生中かどうか.
        /// </summary>
        private bool _isPlaying;

        private void Awake()
        {
            // スキップボタンの登録
            // (シーン上で非アクティブな場合、Start は初回の StartTutorial でアクティブにした後に呼ばれるため Awake で登録する)
            _skipButton.OnSkip += OnSkip;
        }

        private void Start()
        {
            // 初回の StartTutorial でアクティブにされた直後に Start が呼ばれると、
            // ここで非アクティブにすると再生中のチュートリアルが消えてしまうため、再生中は何もしない.
            if (_isPlaying)
            {
                return;
            }

            gameObject.SetActive(false);
        }

        /// <summary>
        /// チュートリアルの表示を開始する
        /// </summary>
        public async UniTask StartTutorial(TutorialKinds kind)
        {
            await LoadAssetAsync(kind);
            _mouseClick = new MouseClick();
            _mouseClick.Enable();
            _cts?.Cancel();
            _cts?.Dispose();
            _cts = new CancellationTokenSource();
            _isPlaying = true;
            gameObject.SetActive(true);
            try
            {
                await ShowTutorialAsync(_cts.Token);
            }
            catch (OperationCanceledException)
            {
                // スキップは正常終了として扱う.
            }
            finally
            {
                _isPlaying = false;
                _mouseClick.Disable();
            }
        }
        
        private async UniTask LoadAssetAsync(TutorialKinds kind)
        {
            var address = string.Format(EchoEdgeConstants.TextBoxMasterDataPath, kind.ToString());
            var textData = await Addressables.LoadAssetAsync<TextBoxMasterData>(address);
            
            if (textData == null)
            {
                Debug.LogWarning($"テキストデータのロードに失敗しました。kind: {kind}");
                return;
            }
            _textData = textData;
        }

        /// <summary>
        /// tutorialを表示する.
        /// </summary>
        /// <param name="cancellationToken">キャンセレーショントークン.</param>
        private async Awaitable ShowTutorialAsync(CancellationToken cancellationToken)
        {
            if(_textData.Models.Count == 0)
            {
                Debug.LogWarning("テキストデータが設定されていません。");
                return;
            }

            foreach (var model in _textData.Models)
            {
                cancellationToken.ThrowIfCancellationRequested();

                // キャラクターのスプライトの設定があればキャラありに、なければ通常のテキストボックスを表示する.
                if (model.Character == null)
                {
                    _currentTextBox = _planeTextBox;
                }
                else
                {
                    _currentTextBox = _charaTextBox;
                    _charaImage.sprite = model.Character;
                }

                _currentTextBox.gameObject.SetActive(true);

                try
                {
                    await _currentTextBox.ShowModelAsync(model, cancellationToken);
                }
                catch(Exception exception)
                {
                    Debug.LogException(exception);
                }

                _currentTextBox.gameObject.SetActive(false);
            }

            gameObject.SetActive(false);
        }

        private void OnSkip()
        {
            _cts?.Cancel();
            _cts?.Dispose();
            _cts = null;
            gameObject.SetActive(false);
        }

        private void OnDestroy()
        {
            _cts?.Cancel();
            _cts?.Dispose();
            _skipButton.OnSkip -= OnSkip;
        }
    }
}