using System.Threading;
using LitMotion;
using LitMotion.Extensions;
using UnityEngine;

namespace CommonUI.Tutorial.Views
{
    public class ForwardingIcon : MonoBehaviour
    {
        [SerializeField]
        private float _basePosX = -73.246f;

        [SerializeField]
        private float _amplitude = 4.354f;

        [SerializeField]
        private float _animDuration = 0.3f;

        [SerializeField]
        private RectTransform _rectTransform;

        /// <summary>
        /// ピンのアニメーションを再生する.
        /// </summary>
        /// <param name="cancellationToken">キャンセレーショントークン.</param>
        public async Awaitable PlayAnimAsync(CancellationToken cancellationToken)
        {
            gameObject.SetActive(true);

            try
            {
                await LMotion.Create(_basePosX, _basePosX + _amplitude, _animDuration)
                    .WithLoops(-1, LoopType.Yoyo)
                    .WithEase(Ease.Linear)
                    .WithOnCancel(() => gameObject.SetActive(false))
                    .BindToAnchoredPositionY(_rectTransform)
                    .ToAwaitable(cancellationToken);
            }
            finally
            {
                gameObject.SetActive(false);
            }
        }
    }
}