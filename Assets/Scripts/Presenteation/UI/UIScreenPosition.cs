using UnityEngine;

namespace EchoEdge.Presenter.UI
{
    /// <summary>
    /// スクリーン座標と UI(RectTransform) のワールド座標を相互変換するユーティリティ。
    /// <para>
    /// Screen Space - Overlay では UI のワールド座標＝スクリーン座標だが、Screen Space - Camera / World Space では一致しない。
    /// キャンバスの Render Mode に関係なく正しい位置になるよう、UI の位置計算ではこのクラスを使う。
    /// </para>
    /// </summary>
    public static class UIScreenPosition
    {
        /// <summary>
        /// <paramref name="rectTransform"/> が属するキャンバスの描画カメラを返す。Overlay の場合は null。
        /// </summary>
        public static Camera GetCanvasCamera(RectTransform rectTransform)
        {
            var canvas = rectTransform.GetComponentInParent<Canvas>();
            if (canvas == null) return null;

            canvas = canvas.rootCanvas;
            return canvas.renderMode == RenderMode.ScreenSpaceOverlay ? null : canvas.worldCamera;
        }

        /// <summary>
        /// <paramref name="rectTransform"/> をスクリーン座標 <paramref name="screenPosition"/> の位置に置く。
        /// </summary>
        public static void SetPositionFromScreen(RectTransform rectTransform, Vector2 screenPosition)
        {
            var reference = rectTransform.parent as RectTransform;
            if (reference == null) reference = rectTransform;

            if (RectTransformUtility.ScreenPointToWorldPointInRectangle(
                    reference, screenPosition, GetCanvasCamera(rectTransform), out var worldPosition))
            {
                rectTransform.position = worldPosition;
            }
        }

        /// <summary>
        /// <paramref name="rectTransform"/> 上のワールド座標 <paramref name="worldPosition"/> をスクリーン座標へ変換する。
        /// </summary>
        public static Vector2 WorldToScreen(RectTransform rectTransform, Vector3 worldPosition)
        {
            return RectTransformUtility.WorldToScreenPoint(GetCanvasCamera(rectTransform), worldPosition);
        }
    }
}
