using UnityEngine;

namespace CommonUI.Tutorial.Views
{
    public abstract class Frame : MonoBehaviour
    {
        [Tooltip("自身のRectTransform")]
        public RectTransform RectTransform;

        /// <summary>
        /// コーチマークの座標に合わせる。
        /// </summary>
        /// <param name="coachMarkPos">対象のコーチマークの位置</param>
        public abstract void SetPosition(RectTransform coachMarkPos);

        /// <summary>
        /// コーチマークのサイズに合わせる。
        /// </summary>
        /// <param name="coachMarkPos">対象のコーチマークの位置</param>
        public abstract void SetSize(RectTransform coachMarkPos);
    }
}