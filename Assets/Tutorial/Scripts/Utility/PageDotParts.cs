using System;
using UnityEngine;
using UnityEngine.UI;

namespace CommonUI.Tutorial.Utility
{
    [Serializable]
    public class PageDotParts
    {
        [SerializeField]
        internal RectTransform _pageIcon;
        [SerializeField]
        internal Image _insideCircle;
        [SerializeField]
        internal RectTransform _outline;
    }
}