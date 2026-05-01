using UnityEngine;

namespace CommonUI.Tutorial.Views
{
    public class RingEffect : MonoBehaviour
    {
        [SerializeField, Tooltip("エフェクトの中心座標を取得するためのRectTransform")]
        private RectTransform _baseRectTransform;

        /// <summary>
        /// エフェクトの大きさの初期値
        /// </summary>
        private const float EffectSize = 100;

        /// <summary>
        /// UIの位置を取得しておくフィールド
        /// </summary>
        private readonly Vector3[] _worldCorners = new Vector3[4];

        /// <summary>
        /// コーチマークの座標に合わせる。
        /// </summary>
        /// <param name="coachMarkPos">コーチマークの座標</param>
        public void SetPosition(RectTransform coachMarkPos)
        {
            // コーチマークのワールド座標を取得する。
            coachMarkPos.GetWorldCorners(_worldCorners);

            // ワールド座標に当てはめる。
            _baseRectTransform.position = (_worldCorners[0] + _worldCorners[2]) / 2;
        }

        /// <summary>
        /// コーチマークのサイズに合わせる。
        /// </summary>
        /// <param name="coachMarkPos">コーチマークの座標</param>
        public void SetSize(RectTransform coachMarkPos)
        {
            // フレームの大きさを設定する。
            // ParticleEffectで作成されているので、コーチマークの大きさに合わせてscaleを変更する。
            var width = coachMarkPos.rect.width;
            var height = coachMarkPos.rect.height;

            // デカイ方を直径とする。
            var diameter = Mathf.Max(width, height);

            // 半径に合わせてアニメーションフレームの大きさを設定する。
            var scale = diameter / EffectSize;
            _baseRectTransform.localScale = new Vector3(scale, scale, 1);
        }
    }
}