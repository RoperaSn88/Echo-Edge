using DG.Tweening;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace EchoEdge.Presenter.UI
{
    /// <summary>
    /// マウスカーソルが乗っている間、対象のCanvasGroupの透明度を下げて背後を見やすくする。
    /// PlayerInfoやAttackInfoなど複数のUIをまとめた親オブジェクトに付与する想定。
    /// </summary>
    [RequireComponent(typeof(CanvasGroup))]
    public class PointerHoverFadeController : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
    {
        private static bool _isActivePointerFading = false;

        /// <summary>
        /// 有効/無効が切り替わった際に通知するイベント。
        /// </summary>
        private static event System.Action<bool> _onActiveChanged;

        /// <summary>
        /// ホバー時の透過処理を有効にするかどうか。
        /// 有効化した瞬間に既にカーソルが乗っていれば、その場で透過させる。
        /// </summary>
        public static bool IsActivePointerFading
        {
            get => _isActivePointerFading;
            set
            {
                if (_isActivePointerFading == value) return;
                _isActivePointerFading = value;
                _onActiveChanged?.Invoke(value);
            }
        }

        /// <summary>
        /// マウスカーソルが乗った際に適用するCanvasGroupのアルファ値。
        /// </summary>
        [SerializeField]
        private float _hoveredAlpha = 0.3f;

        /// <summary>
        /// アルファ値のフェードにかける時間（秒）。
        /// </summary>
        [SerializeField]
        private float _fadeDuration = 0.2f;

        private CanvasGroup _canvasGroup;

        /// <summary>
        /// カーソルが領域内にあるか。透過処理が無効な間も追跡しておく。
        /// </summary>
        private bool _isPointerInside;

        private void Awake()
        {
            _canvasGroup = GetComponent<CanvasGroup>();

            // マウスホバーの検知にはraycastを受け取れるGraphicが必要なため、
            // このオブジェクトにGraphicが無い場合は透明なImageを自動で用意する。
            if (GetComponent<Graphic>() == null)
            {
                var image = gameObject.AddComponent<Image>();
                image.color = new Color(0f, 0f, 0f, 0f);
                image.raycastTarget = true;
            }
        }

        private void OnEnable()
        {
            _onActiveChanged += HandleActiveChanged;
        }

        private void OnDisable()
        {
            _onActiveChanged -= HandleActiveChanged;
            _isPointerInside = false;
        }

        /// <summary>
        /// 透過処理の有効/無効が切り替わった際、カーソルが乗っていれば透明度を即座に反映する。
        /// </summary>
        private void HandleActiveChanged(bool isActive)
        {
            if (!_isPointerInside) return;
            FadeTo(isActive ? _hoveredAlpha : 1f);
        }

        /// <summary>
        /// カーソルが乗った際に、まとめたUIの透明度を下げて背後を見やすくする。
        /// </summary>
        public void OnPointerEnter(PointerEventData eventData)
        {
            _isPointerInside = true;
            if (!_isActivePointerFading) return;
            FadeTo(_hoveredAlpha);
        }

        /// <summary>
        /// カーソルが外れた際に、透明度を元に戻す。
        /// </summary>
        public void OnPointerExit(PointerEventData eventData)
        {
            _isPointerInside = false;
            FadeTo(1f);
        }

        private void FadeTo(float alpha)
        {
            _canvasGroup.DOKill();
            _canvasGroup.DOFade(alpha, _fadeDuration);
        }
    }
}
