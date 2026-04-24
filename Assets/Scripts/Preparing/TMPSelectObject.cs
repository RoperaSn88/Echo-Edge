using TMPro;
using UnityEngine;

namespace UnityEngine
{
    /// <summary>
    /// TextMeshPro用のISelectInterface実装クラス。
    /// 選択時に文字サイズを拡大し、選択解除時に元のサイズに戻す。
    /// </summary>
    public class TMPSelectObject : MonoBehaviour, ISelectInterface
    {
        private const float SelectedFontSize = 150f;
        private const float DeselectedFontSize = 90f;

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
        /// 選択された場合の処理。文字サイズを90から150に変化させ、rect.heightを調整する。
        /// </summary>
        public void OnSelect()
        {
            _text.fontSize = SelectedFontSize;
            var size = _rectTransform.sizeDelta;
            _rectTransform.sizeDelta = new Vector2(size.x, SelectedFontSize);
        }

        /// <summary>
        /// 選択解除された場合の処理。文字サイズを150から90に変化させ、rect.heightを調整する。
        /// </summary>
        public void OnDeselect()
        {
            _text.fontSize = DeselectedFontSize;
            var size = _rectTransform.sizeDelta;
            _rectTransform.sizeDelta = new Vector2(size.x, DeselectedFontSize);
        }
    }
}
