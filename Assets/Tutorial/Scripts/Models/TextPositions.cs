using UnityEngine;

namespace CommonUI.Tutorial.Models
{
    /// <summary>
    /// テキストボックスの位置
    /// </summary>
    public enum TextPositions : byte
    {
        /// <summary>
        /// 左上
        /// </summary>
        [InspectorName("左上")]
        TopLeft,

        /// <summary>
        /// 中央上
        /// </summary>
        [InspectorName("中央上")]
        TopCenter,

        /// <summary>
        /// 右上
        /// </summary>
        [InspectorName("右上")]
        TopRight,

        /// <summary>
        /// 左中央
        /// </summary>
        [InspectorName("左中央")]
        MiddleLeft,

        /// <summary>
        /// 中央
        /// </summary>
        [InspectorName("中央")]
        MiddleCenter,

        /// <summary>
        /// 右中央
        /// </summary>
        [InspectorName("右中央")]
        MiddleRight,

        /// <summary>
        /// 左下
        /// </summary>
        [InspectorName("左下")]
        BottomLeft,

        /// <summary>
        /// 中心下
        /// </summary>
        [InspectorName("中央下")]
        BottomCenter,

        /// <summary>
        /// 右下
        /// </summary>
        [InspectorName("右下")]
        BottomRight
    }
}