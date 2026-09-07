using System.Threading;
using Cysharp.Threading.Tasks;
using DG.Tweening;
using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.Controls;
using UnityEngine.UI;

namespace EchoEdge.Presenter.Preparing
{
    /// <summary>
    /// Preparing シーン起動時のタイトルロゴを表示するクラス。
    /// タイトルロゴと「Press Any Key」を表示し、何らかの操作が行われるまで待機したのち、
    /// 一式を画面左側へ移動させる。移動が完了した後に選択肢が出現する。
    /// </summary>
    public class TitleLogoPresenter : MonoBehaviour
    {
        /// <summary>
        /// タイトルロゴと「Press Any Key」をまとめたグループ
        /// </summary>
        [SerializeField]
        private RectTransform _rectTransform;

        /// <summary>
        /// グループ全体のフェード用
        /// </summary>
        [SerializeField]
        private CanvasGroup _canvasGroup;

        /// <summary>
        /// タイトルロゴ
        /// </summary>
        [SerializeField]
        private Image _titleLogo;

        /// <summary>
        /// 「Press Any Key」のテキスト
        /// </summary>
        [SerializeField]
        private TextMeshProUGUI _pressAnyKeyText;

        [SerializeField]
        [Tooltip("操作を受け付けた後にタイトルロゴ一式を移動させる先の X 座標（アンカー座標）")]
        private float _movedAnchoredPositionX = 150f;

        private const float FadeInDuration = 1.0f;
        private const float MoveDuration = 0.5f;
        private const float BlinkDuration = 0.8f;
        private const float BlinkMinAlpha = 0.15f;

        /// <summary>
        /// 「Press Any Key」の点滅トゥイーン
        /// </summary>
        private Tween _blinkTween;

        private void Reset()
        {
            _rectTransform = GetComponent<RectTransform>();
            _canvasGroup = GetComponent<CanvasGroup>();
        }

        private void Awake()
        {
            if (_rectTransform == null) _rectTransform = GetComponent<RectTransform>();
            if (_canvasGroup == null) _canvasGroup = GetComponent<CanvasGroup>();

            // 表示するまでは見えないようにしておく（プロローグ再生中やフェードイン中に映り込ませない）
            if (_canvasGroup != null) _canvasGroup.alpha = 0f;
        }

        /// <summary>
        /// タイトルロゴを表示し、何らかの操作が行われるまで待機したのち、画面左側へ移動させる。
        /// </summary>
        public async UniTask PresentAsync()
        {
            await ShowAsync(destroyCancellationToken);
            await WaitForAnyInputAsync(destroyCancellationToken);
            await MoveToLeftAsync(destroyCancellationToken);
        }

        /// <summary>
        /// タイトルロゴ一式をフェードインさせ、「Press Any Key」の点滅を開始する。
        /// </summary>
        private async UniTask ShowAsync(CancellationToken cancellationToken)
        {
            gameObject.SetActive(true);
            if (_titleLogo != null) _titleLogo.gameObject.SetActive(true);
            if (_pressAnyKeyText != null) _pressAnyKeyText.gameObject.SetActive(true);

            if (_canvasGroup != null)
            {
                _canvasGroup.alpha = 0f;
                await _canvasGroup.DOFade(1f, FadeInDuration)
                    .SetEase(Ease.OutQuad)
                    .ToUniTask(cancellationToken: cancellationToken);
            }

            StartBlink();
        }

        /// <summary>
        /// 「Press Any Key」の点滅を開始する
        /// </summary>
        private void StartBlink()
        {
            if (_pressAnyKeyText == null) return;

            _blinkTween?.Kill();
            _blinkTween = _pressAnyKeyText.DOFade(BlinkMinAlpha, BlinkDuration)
                .SetEase(Ease.InOutSine)
                .SetLoops(-1, LoopType.Yoyo);
        }

        /// <summary>
        /// 「Press Any Key」の点滅を停止し、完全に表示された状態へ戻す
        /// </summary>
        private void StopBlink()
        {
            _blinkTween?.Kill();
            _blinkTween = null;

            if (_pressAnyKeyText == null) return;

            var color = _pressAnyKeyText.color;
            color.a = 1f;
            _pressAnyKeyText.color = color;
        }

        /// <summary>
        /// 何らかの操作（キー・マウス・タッチ・ゲームパッド）が行われるまで待機する
        /// </summary>
        private async UniTask WaitForAnyInputAsync(CancellationToken cancellationToken)
        {
            // 表示直前の操作でそのまま確定しないよう、1フレーム待ってから判定を始める
            await UniTask.Yield(PlayerLoopTiming.Update, cancellationToken);

            while (!IsAnyInputPressed())
            {
                await UniTask.Yield(PlayerLoopTiming.Update, cancellationToken);
            }
        }

        /// <summary>
        /// このフレームで何らかの操作が行われたかどうか
        /// </summary>
        private static bool IsAnyInputPressed()
        {
            if (Keyboard.current != null && Keyboard.current.anyKey.wasPressedThisFrame)
            {
                return true;
            }

            if (Mouse.current != null &&
                (Mouse.current.leftButton.wasPressedThisFrame
                 || Mouse.current.rightButton.wasPressedThisFrame
                 || Mouse.current.middleButton.wasPressedThisFrame))
            {
                return true;
            }

            if (Touchscreen.current != null && Touchscreen.current.primaryTouch.press.wasPressedThisFrame)
            {
                return true;
            }

            if (Gamepad.current != null)
            {
                foreach (var control in Gamepad.current.allControls)
                {
                    if (control is ButtonControl button && button.wasPressedThisFrame)
                    {
                        return true;
                    }
                }
            }

            return false;
        }

        /// <summary>
        /// タイトルロゴ一式を画面左側へ移動させる
        /// </summary>
        private async UniTask MoveToLeftAsync(CancellationToken cancellationToken)
        {
            StopBlink();

            await _rectTransform.DOAnchorPosX(_movedAnchoredPositionX, MoveDuration)
                .SetEase(Ease.OutQuad)
                .ToUniTask(cancellationToken: cancellationToken);
        }

        private void OnDestroy()
        {
            _blinkTween?.Kill();
            _blinkTween = null;
        }
    }
}
