using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace EchoEdge.Presenter.Preparing
{
    /// <summary>
    /// クリックイベントを通知するデリゲートの型
    /// </summary>
    public delegate void ClickAction(PointerEventData eventData);

    /// <summary>
    /// クリックイベントを持つオブジェクトのインターフェース
    /// </summary>
    public interface IClickableImage
    {
        event ClickAction OnClick;
    }

    /// <summary>
    /// IPointerClickHandlerを実装し、クリック時にOnClickイベントを発火するスクリプト。
    /// Imageオブジェクトに追加して使用する。
    /// <see cref="Interactable"/> を false にすると、クリックを受け付けない状態にし、
    /// 同じオブジェクトの Image を半透明にする。
    /// </summary>
    public class ClickableImage : MonoBehaviour, IPointerClickHandler, IClickableImage
    {
        private const float InteractableAlpha = 1f;
        private const float DefaultNonInteractableAlpha = 0.5f;

        /// <summary>
        /// クリック時に発火するイベント
        /// </summary>
        public event ClickAction OnClick;

        [SerializeField]
        private Image _image;

        /// <summary>
        /// クリック不可時の透明度（Inspectorで個別に調整可能。デフォルトは0.5）
        /// </summary>
        [SerializeField]
        private float _nonInteractableAlpha = DefaultNonInteractableAlpha;

        private bool _interactable = true;

        /// <summary>
        /// クリック可能かどうか。false にするとクリックを受け付けなくなり、半透明表示になる。
        /// </summary>
        public bool Interactable
        {
            get => _interactable;
            set
            {
                if (_interactable == value) return;
                _interactable = value;
                ApplyInteractableAppearance();
            }
        }

        /// <summary>
        /// クリック不可時の透明度。コードから個別のボタンに対して上書きする場合に使用する。
        /// </summary>
        public float NonInteractableAlpha
        {
            get => _nonInteractableAlpha;
            set
            {
                _nonInteractableAlpha = value;
                ApplyInteractableAppearance();
            }
        }

        private void Awake()
        {
            if (_image == null)
            {
                _image = GetComponent<Image>();
            }
        }

        /// <summary>
        /// クリック時に実行される処理
        /// </summary>
        public void OnPointerClick(PointerEventData eventData)
        {
            if (!_interactable) return;
            OnClick?.Invoke(eventData);
        }

        private void ApplyInteractableAppearance()
        {
            if (_image == null) return;

            var color = _image.color;
            color.a = _interactable ? InteractableAlpha : _nonInteractableAlpha;
            _image.color = color;
        }
    }
}
