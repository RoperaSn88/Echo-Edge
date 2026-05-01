using UnityEngine;
using LitMotion;
using LitMotion.Extensions;
using UnityEngine.UI;

namespace CommonUI.Tutorial.Views
{
    public class RectangleFrame : Frame
    {
        [SerializeField, Tooltip("ずっと表示されるフレームの座標")]
        private RectTransform _baseTransform;

        [SerializeField, Tooltip("アニメーションのあるフレームの座標")]
        private RectTransform _animatedRectTransform;

        [SerializeField, Tooltip("アニメーションフレームのImageコンポーネント")]
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
            RectTransform.position = coachMarkPos.position;
        }

        /// <summary>
        /// コーチマークのサイズに合わせる。
        /// </summary>
        /// <param name="coachMarkPos"></param>
        public override void SetSize(RectTransform coachMarkPos)
        {
            // コーチマークのサイズを取得する。
            var width = coachMarkPos.rect.width;
            var height = coachMarkPos.rect.height;

            // ベースフレームの大きさを設定する。
            _baseTransform.SetSizeWithCurrentAnchors(RectTransform.Axis.Horizontal, width);
            _baseTransform.SetSizeWithCurrentAnchors(RectTransform.Axis.Vertical, height);
        }

        /// <summary>
        /// アニメーションフレームの用意をする
        /// </summary>
        /// <param name="coachMarkPos"></param>
        public void SetAnimation(RectTransform coachMarkPos)
        {
            var width = coachMarkPos.rect.width;
            var height = coachMarkPos.rect.height;

            var size = new Vector2(width, height);

            // アニメーションフレームの大きさを設定する。
            // 以下、透過アニメーションを持つフレームについて扱う
            // モーション再生中ならばキャンセルし、新たなモーションを作成する。
            if (_animatedSizeMotionHandle.IsActive())
            {
                _animatedSizeMotionHandle.Cancel();
            }

            // モーションを作成する
            // アニメーションフレームの大きさを設定する。
            _animatedSizeMotionHandle = LMotion.Create(size, size * AnimatedMultiplier, AnimatedTime)
                .WithEase(Ease.OutQuad)
                .WithLoops(-1, LoopType.Restart)
                .Bind(value =>
                {
                    _animatedRectTransform.SetSizeWithCurrentAnchors(RectTransform.Axis.Horizontal, value.x);
                    _animatedRectTransform.SetSizeWithCurrentAnchors(RectTransform.Axis.Vertical, value.y);
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
        /// 非表示になった際にモーションをキャンセルする
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