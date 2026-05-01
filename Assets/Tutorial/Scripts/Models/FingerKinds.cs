using UnityEngine;

namespace CommonUI.Tutorial.Models
{
    /// <summary>
    /// 指の向きを設定する列挙型
    /// </summary>
    public enum FingerKinds
    {
        /// <summary>
        /// 指を上向きにする
        /// </summary>
        [InspectorName("上向き")]
        Top,

        /// <summary>
        /// 指を下向きにする
        /// </summary>
        [InspectorName("下向き")]
        Bottom,

        /// <summary>
        /// 指を左向きにする
        /// </summary>
        [InspectorName("左向き")]
        Left,

        /// <summary>
        /// 指を右向きにする
        /// </summary>
        [InspectorName("右向き")]
        Right
    }
}