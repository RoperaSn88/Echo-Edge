using CommonUI.Tutorial.Models;
using UnityEngine;
using UnityEngine.UI;

namespace CommonUI.Tutorial
{
    /// <summary>
    /// コーチマークのビュー
    /// </summary>
    [RequireComponent(typeof(RectTransform))]
    public class CoachMarkView: MonoBehaviour
    {
        [SerializeField, Tooltip("Imageコンポーネント")]
        private Image _maskImage;

        /// <summary>
        /// マスクの位置
        /// </summary>
        public RectTransform MaskRectTransform => _maskImage.rectTransform;

        [SerializeField, Tooltip("円コーチマークの画像")]
        private Sprite _circleCoachMarkSprite;
        /// <summary>
        /// 円コーチマークの画像
        /// </summary>
        public Sprite CircleCoachMarkSprite => _circleCoachMarkSprite;

        [SerializeField, Tooltip("長方形コーチマークの画像")]
        private Sprite _rectangleCoachMarkSprite;
        /// <summary>
        /// 長方形コーチマークの画像
        /// </summary>
        public Sprite RectangleCoachMarkSprite => _rectangleCoachMarkSprite;

        /// <summary>
        /// コーチマークの形を設定する
        /// </summary>
        /// <param name="kind">指定する形</param>
        public void SetSprite(ShapeKinds kind)
        {
            switch (kind)
            {
                case ShapeKinds.Rectangle:
                    _maskImage.sprite = RectangleCoachMarkSprite;
                    break;
                case ShapeKinds.Circle:
                    _maskImage.sprite = CircleCoachMarkSprite;
                    break;
            }
        }
    }
}