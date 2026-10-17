using System;
using Cysharp.Threading.Tasks;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

using EchoEdge.Domain.Phase;

namespace EchoEdge.Presenter.UI
{
    /// <summary>
    /// プレイヤーフェーズの行動選択ボタン（一閃・装備品・防御・回復）を管理するクラス。
    /// ボタンは縦に4つ並べ、プレイヤーフェーズの間だけ表示する。
    /// </summary>
    public class PlayerPhaseController : MonoBehaviour
    {
        public static PlayerPhaseController Instance { get; private set; }

        /// <summary>
        /// ボタン全体の表示・入力を切り替えるためのCanvasGroup
        /// </summary>
        [SerializeField]
        private CanvasGroup _canvasGroup;

        /// <summary>
        /// 一閃ボタン
        /// </summary>
        [SerializeField]
        private Button _attackButton;

        /// <summary>
        /// 装備品ボタン
        /// </summary>
        [SerializeField]
        private Button _weaponButton;

        /// <summary>
        /// 防御ボタン
        /// </summary>
        [SerializeField]
        private Button _defendButton;

        /// <summary>
        /// 回復ボタン
        /// </summary>
        [SerializeField]
        private Button _healButton;

        /// <summary>
        /// 回復の残り回数を表示するテキスト（未設定でも動作する）
        /// </summary>
        [SerializeField]
        private TextMeshProUGUI _healCountText;

        /// <summary>
        /// 現在の入力待機。待機中でなければ null
        /// </summary>
        private UniTaskCompletionSource<PlayerActionKinds> _selectSource;

        void Awake()
        {
            Instance = this;

            _attackButton.onClick.AddListener(() => OnSelect(PlayerActionKinds.Attack));
            _weaponButton.onClick.AddListener(() => OnSelect(PlayerActionKinds.Weapon));
            _defendButton.onClick.AddListener(() => OnSelect(PlayerActionKinds.Defend));
            _healButton.onClick.AddListener(() => OnSelect(PlayerActionKinds.Heal));

            Hide();
        }

        void OnDestroy()
        {
            if (Instance == this)
            {
                Instance = null;
            }

            _selectSource?.TrySetCanceled();
        }

        /// <summary>
        /// ボタンを表示し、いずれかのボタンが押されるまで待機する。
        /// </summary>
        /// <param name="healRemaining">回復の残り回数。0以下なら回復ボタンを押せなくする</param>
        /// <returns>押されたボタンに対応する行動</returns>
        public async UniTask<PlayerActionKinds> WaitSelectAction(int healRemaining)
        {
            _selectSource = new UniTaskCompletionSource<PlayerActionKinds>();

            _healButton.interactable = healRemaining > 0;
            if (_healCountText != null)
            {
                _healCountText.text = $"残り{Math.Max(0, healRemaining)}回";
            }

            Show();

            try
            {
                using (destroyCancellationToken.Register(() => _selectSource?.TrySetCanceled()))
                {
                    return await _selectSource.Task;
                }
            }
            finally
            {
                _selectSource = null;
                if (this != null)
                {
                    Hide();
                }
            }
        }

        private void OnSelect(PlayerActionKinds kind)
        {
            _selectSource?.TrySetResult(kind);
        }

        private void Show()
        {
            _canvasGroup.alpha = 1f;
            _canvasGroup.interactable = true;
            _canvasGroup.blocksRaycasts = true;
        }

        private void Hide()
        {
            _canvasGroup.alpha = 0f;
            _canvasGroup.interactable = false;
            _canvasGroup.blocksRaycasts = false;
        }
    }
}
