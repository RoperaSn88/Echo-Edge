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

        /// <summary>
        /// カーソルが乗った際に、まとめたUIの透明度を下げて背後を見やすくする。
        /// </summary>
        public void OnPointerEnter(PointerEventData eventData)
        {
            _canvasGroup.DOKill();
            _canvasGroup.DOFade(_hoveredAlpha, _fadeDuration);
        }

        /// <summary>
        /// カーソルが外れた際に、透明度を元に戻す。
        /// </summary>
        public void OnPointerExit(PointerEventData eventData)
        {
            _canvasGroup.DOKill();
            _canvasGroup.DOFade(1f, _fadeDuration);
        }
    }
}
