using System.Threading;
using Cysharp.Threading.Tasks;
using DG.Tweening;
using TMPro;
using UnityEngine;

namespace UnityEngine
{
    /// <summary>
    /// TextMeshPro用のISelectInterface実装クラス。
    /// 選択時に文字サイズをDOTweenでアニメーションさせ、選択解除時に元のサイズに戻す。
    /// </summary>
    public class TMPSelectObject : MonoBehaviour, ISelectInterface
    {
        private const float SelectedFontSize = 150f;
        private const float DeselectedFontSize = 90f;
        private const float TweenDuration = 0.2f;

        [SerializeField]
        private TextMeshProUGUI _text;

        private RectTransform _rectTransform;

        private CancellationTokenSource _cts;

        private void Awake()
        {
            _rectTransform = GetComponent<RectTransform>();
            if (_text == null)
            {
                _text = GetComponent<TextMeshProUGUI>();
            }
            _cts = new CancellationTokenSource();
        }

        private void OnDestroy()
        {
            _cts?.Cancel();
            _cts?.Dispose();
        }

        /// <summary>
        /// 現在進行中のトゥイーンをキャンセルし、新しいCancellationTokenを返す。
        /// </summary>
        private CancellationToken ResetCancellationToken()
        {
            _cts.Cancel();
            _cts.Dispose();
            _cts = new CancellationTokenSource();
            return _cts.Token;
        }

        /// <summary>
        /// カーソルで選択された場合の処理。文字サイズを90から150にDOTweenでアニメーションさせ、rect.heightも調整する。
        /// </summary>
        public async void OnSelect()
        {
            var ct = ResetCancellationToken();
            await UniTask.WhenAll(
                DOTween.To(() => _text.fontSize, x => _text.fontSize = x, SelectedFontSize, TweenDuration).ToUniTask(cancellationToken: ct),
                _rectTransform.DOSizeDelta(new Vector2(_rectTransform.sizeDelta.x, SelectedFontSize), TweenDuration).ToUniTask(cancellationToken: ct)
            );
        }

        /// <summary>
        /// クリックで決定された場合の処理。
        /// </summary>
        public void OnDecide()
        {
        }

        /// <summary>
        /// カーソルの選択が外れた場合の処理。文字サイズを150から90にDOTweenでアニメーションさせ、rect.heightも調整する。
        /// </summary>
        public async void OnDeselect()
        {
            var ct = ResetCancellationToken();
            await UniTask.WhenAll(
                DOTween.To(() => _text.fontSize, x => _text.fontSize = x, DeselectedFontSize, TweenDuration).ToUniTask(cancellationToken: ct),
                _rectTransform.DOSizeDelta(new Vector2(_rectTransform.sizeDelta.x, DeselectedFontSize), TweenDuration).ToUniTask(cancellationToken: ct)
            );
        }
    }
}
