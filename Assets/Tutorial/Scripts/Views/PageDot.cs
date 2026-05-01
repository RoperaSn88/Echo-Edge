using System.Threading;
using CommonUI.Tutorial.Utility;
using UnityEngine;

namespace CommonUI.Tutorial.Views
{
    [RequireComponent(typeof(RectTransform))]
    public class PageDot : MonoBehaviour
    {
        [SerializeField]
        private PageDotParts _parts;

        [SerializeField]
        private float durationSec = 1;

        [SerializeField]
        private PageDotPoint[] _allStates;

        private int _currentStateIndex;

        public PageDot NextDot { get; set; }
        public PageDot PrevDot { get; set; }

        /// <summary>
        /// PageDotを前のステートに進ませる.
        /// </summary>
        /// <param name="cancellationToken">キャンセレーショントークン.</param>
        public async Awaitable NextAsync(CancellationToken cancellationToken)
        {
            if (_currentStateIndex < _allStates.Length - 1)
            {
                await _allStates[_currentStateIndex].AnimateAsync(_allStates[++_currentStateIndex], _parts, cancellationToken);
            }
        }

        /// <summary>
        /// PageDotを次のステートに進ませる.
        /// </summary>
        /// <param name="cancellationToken">キャンセレーショントークン.</param>
        public async Awaitable PrevAsync(CancellationToken cancellationToken)
        {
            if (_currentStateIndex > 0)
            {
                await _allStates[_currentStateIndex].AnimateAsync(_allStates[--_currentStateIndex], _parts, cancellationToken);
            }
        }

        /// <summary>
        /// PageDotを指定のステートに移動させる.
        /// </summary>
        /// <param name="stateIndex">指定のステート.</param>
        public void SetState(int stateIndex)
        {
            _currentStateIndex = stateIndex;
            _allStates[_currentStateIndex].SetState(_parts);
        }
    }
}