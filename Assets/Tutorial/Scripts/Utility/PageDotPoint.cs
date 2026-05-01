using System;
using System.Threading;
using AndanteTribe.Utils.Unity;
using LitMotion;
using Unity.Jobs;
using UnityEngine;

[assembly: RegisterGenericJobType(typeof(MotionUpdateJob<CommonUI.Tutorial.Utility.PageDotPoint, NoOptions, CommonUI.Tutorial.Utility.PageDotPoint.PageDotPointAdapter>))]
namespace CommonUI.Tutorial.Utility
{
    [Serializable]
    public struct PageDotPoint
    {
        public float _posX;
        public Color32 _color;
        public float _insideCircleSize;
        public float _outlineSize;
        public PageDotPoint(float posX, Color color, float insideCircleSize, float outlineSize)
        {
            _posX = posX;
            _color = color;
            _insideCircleSize = insideCircleSize;
            _outlineSize = outlineSize;
        }

        internal readonly struct PageDotPointAdapter : IMotionAdapter<PageDotPoint, NoOptions>
        {
            public PageDotPoint Evaluate(ref PageDotPoint startValue, ref PageDotPoint endValue, ref NoOptions options, in MotionEvaluationContext context)
            {
                var posX = Mathf.Lerp(startValue._posX, endValue._posX, context.Progress);
                var color = Color.LerpUnclamped(startValue._color, endValue._color, context.Progress);
                var insideCircleSize = Mathf.Lerp(startValue._insideCircleSize, endValue._insideCircleSize, context.Progress);
                var outlineSize = Mathf.Lerp(startValue._outlineSize, endValue._outlineSize, context.Progress);
                return new PageDotPoint(posX, color, insideCircleSize, outlineSize);
            }
        }

        public readonly async Awaitable AnimateAsync(PageDotPoint endValue, PageDotParts parts, CancellationToken cancellationToken)
        {
            const float durationSec = 1f;
            await LMotion.Create<PageDotPoint, NoOptions, PageDotPointAdapter>(this, endValue, durationSec)
                .WithEase(Ease.Linear)
                .Bind(parts, static (point, parts) =>
                {
                    parts._pageIcon.anchoredPosition = new Vector2(point._posX, parts._pageIcon.anchoredPosition.y);
                    parts._insideCircle.color = point._color;
                    parts._insideCircle.rectTransform.SetSizeWithCurrentAnchors(RectTransform.Axis.Horizontal, point._insideCircleSize);
                    parts._insideCircle.rectTransform.SetSizeWithCurrentAnchors(RectTransform.Axis.Vertical, point._insideCircleSize);
                    parts._outline.SetSizeWithCurrentAnchors(RectTransform.Axis.Horizontal, point._outlineSize);
                    parts._outline.SetSizeWithCurrentAnchors(RectTransform.Axis.Vertical, point._outlineSize);
                })
                .ToAwaitable(cancellationToken);
        }

        public readonly void SetState(PageDotParts parts)
        {
            parts._pageIcon.anchoredPosition = new Vector2(_posX, parts._pageIcon.anchoredPosition.y);
            parts._insideCircle.color = _color;
            parts._insideCircle.rectTransform.SetSizeWithCurrentAnchors(RectTransform.Axis.Horizontal, _insideCircleSize);
            parts._insideCircle.rectTransform.SetSizeWithCurrentAnchors(RectTransform.Axis.Vertical, _insideCircleSize);
            parts._outline.SetSizeWithCurrentAnchors(RectTransform.Axis.Horizontal, _outlineSize);
            parts._outline.SetSizeWithCurrentAnchors(RectTransform.Axis.Vertical, _outlineSize);
        }
    }
}