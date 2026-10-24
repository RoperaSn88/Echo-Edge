using System;
using Cysharp.Threading.Tasks;
using DG.Tweening;
using TMPro;
using UnityEngine;

using EchoEdge.Domain.Phase;
using UnityEngine.UI;

namespace EchoEdge.Presenter.UI
{

    public class TweenThing
    {
        public RectTransform RectTransform;
        public TextMeshProUGUI Text;
        
        public TweenThing(RectTransform rectTransform, TextMeshProUGUI text)
        {
            RectTransform = rectTransform;
            Text = text;
        }
    }
    
    /// <summary>
    /// プレイヤーフェーズの行動選択ボタン（一閃・装備品・防御・回復）を管理するクラス。
    /// ボタンは縦に4つ並べ、プレイヤーフェーズの間だけ表示する。
    /// </summary>
    public class PlayerPhaseViewController : MonoBehaviour
    {
        public static PlayerPhaseViewController Instance { get; private set; }

        /// <summary>
        /// ボタン全体の表示・入力を切り替えるためのCanvasGroup
        /// </summary>
        [SerializeField]
        private CanvasGroup _canvasGroup;

        /// <summary>
        /// 一閃ボタン
        /// </summary>
        [SerializeField]
        private PhaseButton _attackButton;

        /// <summary>
        /// 装備品ボタン
        /// </summary>
        [SerializeField]
        private PhaseButton _weaponButton;

        /// <summary>
        /// 防御ボタン
        /// </summary>
        [SerializeField]
        private PhaseButton _defendButton;

        /// <summary>
        /// 回復ボタン
        /// </summary>
        [SerializeField]
        private PhaseButton _healButton;

        /// <summary>
        /// 回復の残り回数を表示するテキスト（未設定でも動作する）
        /// </summary>
        [SerializeField]
        private TextMeshProUGUI _healCountText;
        
        [SerializeField]
        private VerticalLayoutGroup _verticalLayoutGroup;
        
        private TweenThing[] _tweenThings;

        private const float ShownAlpha = 1f;
        private const float HiddenAlpha = 0f;
        private const float SlideStartPosX = 600f;
        private const float SlideEndPosX = 450f;
        private const float SlideTime = 0.3f;
        private const float SlideInterval = 0.05f;
        private const float FadeTime = 0.2f;

        /// <summary>
        /// まだ一度もボタンを表示していないか。初回のみ演出付きで表示する
        /// </summary>
        private bool _isFirstShow = true;

        /// <summary>
        /// 現在の入力待機。待機中でなければ null
        /// </summary>
        private UniTaskCompletionSource<PlayerActionKinds> _selectSource;

        void Awake()
        {
            Instance = this;

            SetupButton(_attackButton, PlayerActionKinds.Attack);
            SetupButton(_weaponButton, PlayerActionKinds.Weapon);
            SetupButton(_defendButton, PlayerActionKinds.Defend);
            SetupButton(_healButton, PlayerActionKinds.Heal);

            _tweenThings = new TweenThing[]
            {
                new TweenThing(_attackButton.RectTransform, _attackButton.Text),
                new TweenThing(_weaponButton.RectTransform, _weaponButton.Text),
                new TweenThing(_defendButton.RectTransform, _defendButton.Text),
                new TweenThing(_healButton.RectTransform, _healButton.Text)
            };
            
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
        /// <remarks>初回はボタンを1つずつスライドインさせ、2回目以降はフェードインで表示する</remarks>
        /// <returns>押されたボタンに対応する行動</returns>
        public async UniTask<PlayerActionKinds> WaitSelectAction(int healRemaining)
        {
            _selectSource = new UniTaskCompletionSource<PlayerActionKinds>();

            _healButton.Interactable = healRemaining > 0;
            if (_healCountText != null)
            {
                _healCountText.text = $"{Math.Max(0, healRemaining)}";
            }

            if (_isFirstShow)
            {
                _isFirstShow = false;
                await ShowLongAsync();
            }
            else
            {
                await ShowShortAsync();
            }

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

        /// <summary>
        /// ボタンにクリック・カーソル重なり・カーソル離脱の処理を代入する。
        /// </summary>
        private void SetupButton(PhaseButton button, PlayerActionKinds kind)
        {
            button.OnClick = () => OnSelect(kind);
            button.Initialize();
        }

        private void OnSelect(PlayerActionKinds kind)
        {
            _selectSource?.TrySetResult(kind);
        }

        private async UniTask ShowLongAsync()
        {
            _verticalLayoutGroup.enabled = false;

            foreach (var thing in _tweenThings)
            {
                thing.RectTransform.anchoredPosition = new Vector2(SlideStartPosX, thing.RectTransform.anchoredPosition.y);
                thing.Text.color = new Color(thing.Text.color.r, thing.Text.color.g, thing.Text.color.b, 0);
            }

            _canvasGroup.alpha = ShownAlpha;

            foreach (var thing in _tweenThings)
            {
                thing.RectTransform.DOAnchorPosX(SlideEndPosX, SlideTime).SetEase(Ease.OutQuad);
                thing.Text.DOFade(ShownAlpha, SlideTime).SetEase(Ease.OutQuad);
                await UniTask.Delay(TimeSpan.FromSeconds(SlideInterval));
            }
            await UniTask.Delay(TimeSpan.FromSeconds(SlideTime));
            
            _verticalLayoutGroup.enabled = true;
            _canvasGroup.interactable = true;
            _canvasGroup.blocksRaycasts = true;
        }
        
        private async UniTask ShowShortAsync()
        {
            _verticalLayoutGroup.enabled = true;
            foreach (var thing in _tweenThings)
            {
                thing.Text.color = new Color(thing.Text.color.r, thing.Text.color.g, thing.Text.color.b, 1);
            }
            
            await DOTween.To(() => _canvasGroup.alpha, x => _canvasGroup.alpha = x, ShownAlpha, FadeTime).SetEase(Ease.OutQuad).ToUniTask();
            _canvasGroup.interactable = true;
            _canvasGroup.blocksRaycasts = true;
        }

        private void Hide()
        {
            _canvasGroup.alpha = HiddenAlpha;
            _canvasGroup.interactable = false;
            _canvasGroup.blocksRaycasts = false;

            foreach (var thing in _tweenThings)
            {
                thing.Text.color = new Color(thing.Text.color.r, thing.Text.color.g, thing.Text.color.b, 0);
            }
        }
    }
}
