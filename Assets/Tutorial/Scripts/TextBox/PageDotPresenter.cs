using System;
using System.Threading;
using CommonUI.Tutorial.Views;
using UnityEngine;
using UnityEngine.Pool;

namespace CommonUI.Tutorial
{
    public class PageDotPresenter : MonoBehaviour
    {
        [SerializeField, Tooltip("ページ表示のPrefab")]
        private PageDot _dotPrefab;
        [SerializeField, Tooltip("ドットの親のRectTransform")]
        private RectTransform _dotRoot;
        [SerializeField, Tooltip("中心から左右にあるドットの数")]
        private int _sideDotCount = 3;
        [SerializeField, Tooltip("現在ページを表すステートの番号")]
        private int _currentPageState = 4;

        private int _totalPages;
        private int _currentPageIndex;
        private int _rightPageIndex;
        private int _leftPageIndex;

        private ObjectPool<PageDot> _pool;
        private PageDot _head;
        private PageDot _tail;
        private int _dotsCount;

        private CancellationTokenSource _cts;

        /// <summary>
        /// 全体ページ数に合わせてページドットを初期化する.
        /// </summary>
        /// <param name="totalPages">全体ページ数.</param>
        public void Initialize(int totalPages)
        {
            _pool ??= new ObjectPool<PageDot>(
                createFunc: () => Instantiate(_dotPrefab, _dotRoot),
                actionOnGet: dot => dot.gameObject.SetActive(true),
                actionOnRelease: dot => dot.gameObject.SetActive(false),
                defaultCapacity: 8
            );

            for (var dot = _head; dot != null; dot = dot.NextDot)
            {
                _pool.Release(dot);
            }
            _head = null;
            _tail = null;
            _dotsCount = 0;

            _totalPages = totalPages;
            _currentPageIndex = 0;
            _leftPageIndex = 0;

            var targetDotCount = Mathf.Min(_sideDotCount + 1, totalPages);
            if (targetDotCount <= 0)
            {
                return;
            }

            // 1つめのDotを確保.
            _head = _pool.Get();
            _head.SetState(_currentPageState);
            _head.NextDot = null;
            _head.PrevDot = null;
            _tail = _head;
            _dotsCount = 1;

            // 2つめ以降はループして確保.
            for (var i = 1; i < targetDotCount; i++)
            {
                var dot = _pool.Get();
                dot.SetState(_currentPageState - i);
                dot.NextDot = null;
                dot.PrevDot = _tail;

                _tail.NextDot = dot;
                _tail = dot;
                _dotsCount++;
            }

            _rightPageIndex = _dotsCount - 1;
        }

        /// <summary>
        /// ドットすべてを1つ次に進める.
        /// </summary>
        public void Next()
        {
            if (_head == null || _currentPageIndex >= _totalPages - 1)
            {
                return;
            }

            _cts?.Cancel();
            _cts?.Dispose();
            _cts = new CancellationTokenSource();

            _currentPageIndex++;

            // アニメーションが終わったものを消したいので一つ余分に確保.
            var leftDotsCount = _sideDotCount + 1;

            if (_leftPageIndex < _currentPageIndex - leftDotsCount)
            {
                var oldHead = _head;
                _head = _head.NextDot;

                if (_head != null)
                {
                    _head.PrevDot = null;
                }
                else
                {
                    _tail = null;
                }

                oldHead.NextDot = null;
                oldHead.PrevDot = null;
                _pool.Release(oldHead);

                _dotsCount--;
                _leftPageIndex++;
            }

            // 現在ページから見て、右に見えるはずのドットを足す
            if (_rightPageIndex < _totalPages - 1 && _rightPageIndex < _currentPageIndex + leftDotsCount)
            {
                var newDot = _pool.Get();
                var enterState = _currentPageState - leftDotsCount;
                newDot.SetState(enterState);

                newDot.NextDot = null;
                newDot.PrevDot = _tail;

                if (_tail != null)
                {
                    _tail.NextDot = newDot;
                }
                _tail = newDot;
                if (_head == null)
                {
                    _head = newDot;
                }

                _dotsCount++;
                _rightPageIndex++;
            }

            // dotすべてのアニメーションを起動.
            for (var dot = _head; dot != null; dot = dot.NextDot)
            {
                _ = dot.NextAsync(_cts.Token);
            }
        }

        /// <summary>
        /// ドットすべてを1つ前に進める.
        /// </summary>
        public void Prev()
        {
            if (_head == null || _currentPageIndex <= 0)
            {
                return;
            }

            _cts?.Cancel();
            _cts?.Dispose();
            _cts = new CancellationTokenSource();

            _currentPageIndex--;

            var rightDotsCount = _sideDotCount + 1;

            if (_rightPageIndex > _currentPageIndex + rightDotsCount)
            {
                var oldTail = _tail;
                _tail = _tail.PrevDot;

                if (_tail != null)
                {
                    _tail.NextDot = null;
                }
                else
                {
                    _head = null;
                }

                oldTail.NextDot = null;
                oldTail.PrevDot = null;
                _pool.Release(oldTail);

                _dotsCount--;
                _rightPageIndex--;
            }

            // dotすべてのアニメーションを起動.
            for (var dot = _head; dot != null; dot = dot.NextDot)
            {
                _ = dot.PrevAsync(_cts.Token);
            }

            // 現在ページから見て、左に見えるはずのドットを足す.
            if (_leftPageIndex > 0 && _leftPageIndex > _currentPageIndex - rightDotsCount)
            {
                var newDot = _pool.Get();

                var enterState = _currentPageState  + rightDotsCount;
                newDot.SetState(enterState);

                newDot.PrevDot = null;
                newDot.NextDot = _head;

                if (_head != null)
                {
                    _head.PrevDot = newDot;
                }

                _head = newDot;

                if (_tail == null)
                {
                    _tail = newDot;
                }

                _dotsCount++;
                _leftPageIndex--;
            }
        }

        private void OnDestroy()
        {
            _cts?.Cancel();
            _cts?.Dispose();
        }
    }
}