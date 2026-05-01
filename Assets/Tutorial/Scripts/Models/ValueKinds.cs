using UnityEngine;

namespace CommonUI.Tutorial.Models
{
    /// <summary>
    /// 座標をどれくらいズラすか
    /// </summary>
    public enum ValueKinds : int
    {
        /// <summary>
        /// ズラさない
        /// </summary>
        [InspectorName("0")]
        Zero = 0,

        /// <summary>
        /// 100pxズラす
        /// </summary>
        [InspectorName("+100")]
        Small = 100,

        /// <summary>
        /// 200pxズラす
        /// </summary>
        [InspectorName("+200")]
        Medium = 200,

        /// <summary>
        /// 300pxズラす
        /// </summary>
        [InspectorName("+300")]
        Large = 300,

        /// <summary>
        /// 400pxズラす
        /// </summary>
        [InspectorName("+400")]
        VeryLarge = 400,

        /// <summary>
        /// -100pxズラす
        /// </summary>
        [InspectorName("-100")]
        NegativeSmall = -100,

        /// <summary>
        /// -200pxズラす
        /// </summary>
        [InspectorName("-200")]
        NegativeMedium = -200,

        /// <summary>
        /// -300pxズラす
        /// </summary>
        [InspectorName("-300")]
        NegativeLarge = -300,

        /// <summary>
        /// -400pxズラす
        /// </summary>
        [InspectorName("-400")]
        NegativeVeryLarge = -400
    }
}