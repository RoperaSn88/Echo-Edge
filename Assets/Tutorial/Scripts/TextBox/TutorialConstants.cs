using UnityEngine;

namespace CommonUI.Tutorial
{
    public static class TutorialConstants
    {
        public static class TextBoxPositions
        {
            /// <summary>
            /// 左上に配置するためのベクトル
            /// </summary>
            public static readonly Vector2 TopLeftVector = new Vector2(0, 1);

            /// <summary>
            /// 中央上に配置するためのベクトル
            /// </summary>
            public static readonly Vector2 TopCenterVector = new Vector2(0.5f, 1);

            /// <summary>
            /// 右上に配置するためのベクトル
            /// </summary>
            public static readonly Vector2 TopRightVector = new Vector2(1, 1);

            /// <summary>
            /// 中央左に配置するためのベクトル
            /// </summary>
            public static readonly Vector2 MiddleLeftVector = new Vector2(0, 0.5f);

            /// <summary>
            /// 中央に配置するためのベクトル
            /// </summary>
            public static readonly Vector2 MiddleCenterVector = new Vector2(0.5f, 0.5f);

            /// <summary>
            /// 中央右に配置するためのベクトル
            /// </summary>
            public static readonly Vector2 MiddleRightVector = new Vector2(1, 0.5f);

            /// <summary>
            /// 左下に配置するためのベクトル
            /// </summary>
            public static readonly Vector2 BottomLeftVector = new Vector2(0, 0);

            /// <summary>
            /// 中央下に配置するためのベクトル
            /// </summary>
            public static readonly Vector2 BottomCenterVector = new Vector2(0.5f, 0);

            /// <summary>
            /// 右下に配置するためのベクトル
            /// </summary>
            public static readonly Vector2 BottomRightVector = new Vector2(1, 0);
        }

        /// <summary>
        /// 指アイコンの回転を設定するためのクラス
        /// </summary>
        public sealed class FingerRotations
        {
            /// <summary>
            /// 上向きにするためのクォータニオン
            /// </summary>
            public static readonly Quaternion UpperQuaternion = Quaternion.Euler(0, 0, 0);

            /// <summary>
            /// 下向きにするためのクォータニオン
            /// </summary>
            public static readonly Quaternion BottomQuaternion = Quaternion.Euler(0, 0, 180);

            /// <summary>
            /// 左向きにするためのクォータニオン
            /// </summary>
            public static readonly Quaternion LeftQuaternion = Quaternion.Euler(0, 0, 90);

            /// <summary>
            /// 右向きにするためのクォータニオン
            /// </summary>
            public static readonly Quaternion RightQuaternion = Quaternion.Euler(0, 0, 270);
        }
    }
}