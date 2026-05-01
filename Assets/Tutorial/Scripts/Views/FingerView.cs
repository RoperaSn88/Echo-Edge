using CommonUI.Tutorial.Models;
using UnityEngine;

namespace CommonUI.Tutorial.Views
{
    /// <summary>
    /// 指アイコンの見た目に関するクラス
    /// </summary>
    public class FingerView : MonoBehaviour
    {
        [SerializeField, Tooltip("指の位置")]
        private RectTransform _rectTransform;

        [SerializeField, Tooltip("指アイコンをコーチマークからどれくらい離すかのオフセット値")]
        private float _fingerOffset = 100f;

        /// <summary>
        /// 指アイコンを設定する
        /// </summary>
        /// <param name="maskRectTransform">コーチマークのRectTransform</param>
        /// <param name="kind">指アイコンの向き</param>
        public void SetIcon(RectTransform maskRectTransform, FingerKinds kind)
        {
            SetPosition(maskRectTransform, kind);

            SetDirection(kind);
        }

        /// <summary>
        /// 指アイコンの位置を設定する
        /// </summary>
        /// <param name="maskRectTransform">コーチマークのRectTransform</param>
        /// <param name="kind">指アイコンの向き</param>
        void SetPosition(RectTransform maskRectTransform, FingerKinds kind)
        {
            // コーチマークの中心を取得
            var resultPosition = maskRectTransform.anchoredPosition;

            // コーチマークの大きさを取得
            var width = maskRectTransform.rect.width;
            var height = maskRectTransform.rect.height;

            switch (kind)
            {
                case FingerKinds.Top:
                    // コーチマークの中心から下に上向きの指アイコンを配置
                    resultPosition += new Vector2(0, -(height / 2) - _fingerOffset);
                    break;
                case FingerKinds.Bottom:
                    // コーチマークの中心から上に下向きの指アイコンを配置
                    resultPosition += new Vector2(0, (height / 2) + _fingerOffset);
                    break;
                case FingerKinds.Left:
                    // コーチマークの中心から右に左向きの指アイコンを配置
                    resultPosition += new Vector2((width / 2) + _fingerOffset, 0);
                    break;
                case FingerKinds.Right:
                    // コーチマークの中心から左に右向きの指アイコンを配置
                    resultPosition += new Vector2(-(width / 2) - _fingerOffset, 0);
                    break;
            }

            // 計算結果に指アイコンを配置
            _rectTransform.anchoredPosition = resultPosition;
        }

        /// <summary>
        /// 指アイコンの向きを設定する
        /// </summary>
        /// <param name="kind">指アイコンの向き</param>
        void SetDirection(FingerKinds kind)
        {
            switch (kind)
            {
                case FingerKinds.Top:
                    _rectTransform.localRotation = TutorialConstants.FingerRotations.UpperQuaternion;
                    break;
                case FingerKinds.Bottom:
                    _rectTransform.localRotation = TutorialConstants.FingerRotations.BottomQuaternion;
                    break;
                case FingerKinds.Left:
                    _rectTransform.localRotation = TutorialConstants.FingerRotations.LeftQuaternion;
                    break;
                case FingerKinds.Right:
                    _rectTransform.localRotation = TutorialConstants.FingerRotations.RightQuaternion;
                    break;
            }
        }
    }
}