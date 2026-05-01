using Coffee.UISoftMask;
using Coffee.UISoftMaskInternal;
using UnityEngine;
using UnityEngine.Assertions;
using UnityEngine.UI;

namespace CommonUI.Tutorial
{
    /// <summary>
    /// SoftMaskの処理をラップして，コーチマークのマスクくり抜きを行うクラス
    /// </summary>
    public class MaskCutout : MonoBehaviour
    {
        [SerializeField, Tooltip("四角形のマスクくり抜き用のスプライト")]
        private Sprite _submaskRectangleSprite;

        [SerializeField, Tooltip("円形のマスクくり抜き用のスプライト")]
        private Sprite _submaskCircleSprite;

        [SerializeField, Tooltip("SubtractオブジェクトのRectTransform")]
        private RectTransform _subtractRectTransform;

        [SerializeField, Tooltip("SubtractオブジェクトのImage")]
        private Image _subtractImage;

        [SerializeField, Tooltip("SubtractオブジェクトのMaskingShape")]
        private MaskingShape _subtractMaskingShape;

        /// <summary>
        /// 四角形にマスクをくり抜く処理
        /// </summary>
        /// <param name="centerX">くり抜きの中心X座標</param>
        /// <param name="centerY">くり抜きの中心Y座標</param>
        /// <param name="width">くり抜きの横幅（0以上）</param>
        /// <param name="height">くり抜きの縦幅（0以上）</param>
        /// <param name="paddingRatio">くり抜き部分からのパディング [0, 1]　※ 0 &lt; paddingRatio + gradientRatio &lt;= 1</param>
        /// <param name="gradientRatio">グラデーションの幅 [0, 1]　※ 0 &lt; paddingRatio + gradientRatio &lt;= 1</param>
        public void SetRectangle(float centerX, float centerY, float width, float height,
            float paddingRatio, float gradientRatio)
        {
            Assert.IsTrue(width >= 0, $"[MaskCutout] width は 0 以上である必要があります: {width}");
            Assert.IsTrue(height >= 0, $"[MaskCutout] height は 0 以上である必要があります: {height}");


            _subtractImage.sprite = _submaskRectangleSprite;

            (width, height) = CalculateCenterSize(_subtractImage, width, height);
            SetupRectTransform(centerX, centerY, width, height);
            ApplySoftnessRange(paddingRatio, gradientRatio);
        }

        /// <summary>
        /// 円形にマスクをくり抜く処理
        /// </summary>
        /// <param name="centerX">くり抜きの中心X座標</param>
        /// <param name="centerY">くり抜きの中心Y座標</param>
        /// <param name="diameter">くり抜きの直径（0以上）</param>
        /// <param name="paddingRatio">くり抜き部分からのパディング [0, 1]　※ 0 &lt; paddingRatio + gradientRatio &lt;= 1</param>
        /// <param name="gradientRatio">グラデーションの幅 [0, 1]　※ 0 &lt; paddingRatio + gradientRatio &lt;= 1</param>
        public void SetCircle(float centerX, float centerY, float diameter,
            float paddingRatio, float gradientRatio)
        {
            Assert.IsTrue(diameter >= 0, $"[MaskCutout] diameter は 0 以上である必要があります: {diameter}");

            _subtractImage.sprite = _submaskCircleSprite;

            diameter = CalculateCenterSize(diameter);
            SetupRectTransform(centerX, centerY, diameter, diameter);
            ApplySoftnessRange(paddingRatio, gradientRatio);
        }

        /// <summary>
        /// RectTransformへの設定値適用処理
        /// </summary>
        private void SetupRectTransform(float centerX, float centerY, float rectWidth, float rectHeight)
        {
            // アンカーとピボットは中心(0.5, 0.5)固定
            var center = new Vector2(0.5f, 0.5f);
            _subtractRectTransform.anchorMin = center;
            _subtractRectTransform.anchorMax = center;
            _subtractRectTransform.pivot     = center;

            _subtractRectTransform.anchoredPosition = new Vector2(centerX, centerY);

            _subtractRectTransform.SetSizeWithCurrentAnchors(RectTransform.Axis.Horizontal, rectWidth);
            _subtractRectTransform.SetSizeWithCurrentAnchors(RectTransform.Axis.Vertical,   rectHeight);
        }

        /// <summary>
        /// paddingRatio と gradientRatio から softnessRange を計算して適用する処理
        /// </summary>
        private void ApplySoftnessRange(float paddingRatio, float gradientRatio)
        {
            Assert.IsTrue(paddingRatio >= 0f, $"[MaskCutout] paddingRatio は 0 以上である必要があります: {paddingRatio}");
            Assert.IsTrue(gradientRatio >= 0f, $"[MaskCutout] gradientRatio は 0 以上である必要があります: {gradientRatio}");
            Assert.IsTrue(paddingRatio + gradientRatio > 0f && paddingRatio + gradientRatio <= 1f, $"[MaskCutout] 0 < paddingRatio + gradientRatio <= 1 である必要があります: {paddingRatio + gradientRatio}");

            var softMax = 1f - paddingRatio;
            var softMin = 1f - paddingRatio - gradientRatio;

            _subtractMaskingShape.softnessRange = new MinMax01(softMin, softMax);
        }


        /// <summary>
        /// Sliced Imageの「中央（完全透過部分）」が指定サイズになるようにRectTransformのサイズを計算する処理
        /// </summary>
        private static (float width, float height) CalculateCenterSize(Image slicedImage, float centerWidth, float centerHeight)
        {
            var border = slicedImage.sprite.border / slicedImage.pixelsPerUnit;

            // 中央サイズ + 枠サイズ = 全体サイズ
            return (centerWidth  + border.x + border.z,
                    centerHeight + border.y + border.w);
        }

        /// <summary>
        /// 透過用スプライトの「中央（完全透過部分）」が指定サイズになるように全体サイズを計算する処理
        /// </summary>
        private static float CalculateCenterSize(float centerDiameter) => centerDiameter * 2;
    }
}
