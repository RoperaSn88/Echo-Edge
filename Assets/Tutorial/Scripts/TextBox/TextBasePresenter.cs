using System;
using System.Threading;
using CommonUI.Tutorial.Models;
using CommonUI.Tutorial.Views;
using UnityEngine;
using UnityEngine.UI;

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

        private void Start()
        {
            // スキップボタンの登録
            _skipButton.OnSkip += OnSkip;

            gameObject.SetActive(false);
        }

        /// <summary>
        /// チュートリアルの表示を開始する
        /// </summary>
        public void StartTutorial()
        {
            gameObject.SetActive(true);
            _ = ShowTutorialAsync(_cts.Token);
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