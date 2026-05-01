using System;
using System.Threading;
using LitMotion;
using LitMotion.Extensions;
using TMPro;
using UnityEngine;

namespace CommonUI.Tutorial.Views
{
    /// <summary>
    /// <see cref="TextMeshProUGUI"/>に文字送りの拡張を提供するクラス.
    /// </summary>
    public static class TextAnimationExtention
    {
        /// <summary>
        /// 1文字ずつ表示させるメソッド.
        /// </summary>
        /// <param name="textMeshProUGUI">文字送りさせる対象.</param>
        /// <param name="value">文字送りさせたいテキスト.</param>
        /// <param name="interval">表示にかかる1文字あたりの秒.</param>
        /// <param name="cancellationToken">キャンセルトークン.</param>
        public static Awaitable AnimateTextAsync(this TextMeshProUGUI textMeshProUGUI, string value, TimeSpan interval, CancellationToken cancellationToken)
        {
            textMeshProUGUI.text = value;

            return LMotion.Create(0, value.Length, value.Length * (float)interval.TotalSeconds)
                .WithEase(Ease.Linear)
                .BindToMaxVisibleCharacters(textMeshProUGUI)
                .ToAwaitable(CancelBehavior.Complete, cancellationToken);
        }
    }
}