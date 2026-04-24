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

        private void Awake()
        {
            _rectTransform = GetComponent<RectTransform>();
            if (_text == null)
            {
                _text = GetComponent<TextMeshProUGUI>();
            }
        }

        /// <summary>
        /// カーソルで選択された場合の処理。文字サイズを90から150にDOTweenでアニメーションさせ、rect.heightも調整する。
        /// </summary>
        public void OnSelect()
        {
            DOTween.To(() => _text.fontSize, x => _text.fontSize = x, SelectedFontSize, TweenDuration);
            _rectTransform.DOSizeDelta(new Vector2(_rectTransform.sizeDelta.x, SelectedFontSize), TweenDuration);
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
        public void OnDeselect()
        {
            DOTween.To(() => _text.fontSize, x => _text.fontSize = x, DeselectedFontSize, TweenDuration);
            _rectTransform.DOSizeDelta(new Vector2(_rectTransform.sizeDelta.x, DeselectedFontSize), TweenDuration);
        }
    }
}
