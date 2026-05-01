using LitMotion;
using LitMotion.Extensions;
using UnityEngine;
using UnityEngine.UI;

namespace CommonUI.Tutorial.Views
{
    public class CircleFrame : Frame
    {
        [SerializeField, Tooltip("ずっと表示されるフレームの座標")]
        private RectTransform _baseTransform;

        [SerializeField, Tooltip("アニメーションのあるフレームの座標")]
        private RectTransform _animatedRectTransform;

        [SerializeField, Tooltip("フレームのイメージ")]
        private Image _animatedImage;

        /// <summary>
        /// モーションに使用する時間
        /// </summary>
        private const float AnimatedTime = 1f;

        /// <summary>
        /// アニメーションフレームの大きさをどれくらい大きくするかの倍率
        /// </summary>
        private const float AnimatedMultiplier = 1.3f;

        /// <summary>
        /// UIの位置を取得しておくフィールド
        /// </summary>
        private readonly Vector3[] _worldCorners = new Vector3[4];

        /// <summary>
        /// 動きのあるフレームのモーションハンドル
        /// </summary>
        private MotionHandle _animatedSizeMotionHandle;

        /// <summary>
        /// 透過アニメーションのモーションハンドル
        /// </summary>
        private MotionHandle _animatedAlphaMotionHandle;

        /// <summary>
        /// コーチマークの座標に合わせる。
        /// </summary>
        /// <param name="coachMarkPos">コーチマークの座標</param>
        public override void SetPosition(RectTransform coachMarkPos)
        {
            // コーチマークのワールド座標を取得する。
            coachMarkPos.GetWorldCorners(_worldCorners);

            // ワールド座標に当てはめる。
            RectTransform.position = (_worldCorners[0] + _worldCorners[2]) / 2;
        }

        /// <summary>
        /// コーチマークのサイズに合わせる。
        /// </summary>
        /// <param name="coachMarkPos">コーチマークの座標</param>
        public override void SetSize(RectTransform coachMarkPos)
        {
            // フレームの大きさを設定する。
            var width = coachMarkPos.rect.width;
            var height = coachMarkPos.rect.height;

            // デカイ方を半径とする。
            var radius = Mathf.Max(width, height);

            // ベースフレームの大きさを設定する。
            _baseTransform.SetSizeWithCurrentAnchors(RectTransform.Axis.Horizontal, radius);
            _baseTransform.SetSizeWithCurrentAnchors(RectTransform.Axis.Vertical, radius);
        }

        /// <summary>
        /// アニメーションフレームの用意をする
        /// </summary>
        /// <param name="coachMarkPos"></param>
        public void SetAnimation(RectTransform coachMarkPos)
        {
            var width = coachMarkPos.rect.width;
            var height = coachMarkPos.rect.height;

            var diameter = Mathf.Max(width, height);

            // アニメーションフレームの大きさを設定する。
            // 以下、透過アニメーションを持つフレームについて扱う
            // モーション再生中ならばキャンセルし、新たなモーションを作成する。
            if (_animatedSizeMotionHandle.IsActive())
            {
                _animatedSizeMotionHandle.Cancel();
            }

            // モーションを作成する
            // アニメーションフレームの大きさを設定する。
            _animatedSizeMotionHandle = LMotion.Create(diameter, diameter * AnimatedMultiplier, AnimatedTime)
                .WithEase(Ease.OutQuad)
                .WithLoops(-1, LoopType.Restart)
                .Bind(value =>
                {
                    _animatedRectTransform.SetSizeWithCurrentAnchors(RectTransform.Axis.Horizontal, value);
                    _animatedRectTransform.SetSizeWithCurrentAnchors(RectTransform.Axis.Vertical, value);
                })
                .AddTo(this);

            // 透過アニメーションも同様に実装する
            if(_animatedAlphaMotionHandle.IsActive())
            {
                _animatedAlphaMotionHandle.Cancel();
            }
            _animatedAlphaMotionHandle = LMotion.Create(1f, 0f, AnimatedTime)
                .WithEase(Ease.OutQuad)
                .WithLoops(-1, LoopType.Restart)
                .BindToColorA(_animatedImage);
        }

        /// <summary>
        /// 非表示にする際はモーションをキャンセルする。
        /// </summary>
        public void OnDisable()
        {
            // モーションが再生されている場合はキャンセルする。
            if (_animatedSizeMotionHandle.IsActive())
            {
                _animatedSizeMotionHandle.Cancel();
            }

            if (_animatedAlphaMotionHandle.IsActive())
            {
                _animatedAlphaMotionHandle.Cancel();
            }
        }
    }
}