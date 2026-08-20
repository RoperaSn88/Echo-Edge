using System;
using System.Threading;
using Applicatiton.Scenario;
using Cysharp.Threading.Tasks;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using DG.Tweening;
using EchoEdge.Utility;

namespace Domain.Scenario.Controller
{
    /// <summary>
    /// シナリオの表示を担当するビュークラス。
    /// 表示することに専念し、条件分岐やデータ取得などのロジックは持たない。
    /// どの内容をいつ表示するかの判断は <see cref="ScenarioViewController"/> が行い、
    /// このクラスは渡された値をそのまま画面に反映するだけの役割を持つ。
    /// </summary>
    public class ScenarioView : MonoBehaviour
    {
        [SerializeField] private TextMeshProUGUI _speakerText;
        [SerializeField] private TextMeshProUGUI _bodyText;
        [SerializeField] private Image _leftCharacterImage;
        [SerializeField] private Image _rightCharacterImage;
        [SerializeField] private Image _backgroundImage;
        [SerializeField] private Image _fadeImage;
        
        private const float FadeDuration = 0.5f;
        private const float HighlightedBrightness = 1f;
        private const float DimmedBrightness = 0.5f;

        /// <summary>
        /// セリフ（話者名と本文）を表示する。
        /// </summary>
        public void ShowPhrase(string speakerName, string body)
        {
            _speakerText.text = speakerName;
            _bodyText.text = body;
        }

        /// <summary>
        /// 指定した位置にキャラクターのスプライトを表示する。
        /// キャラクター登場・表情変更のいずれも、このメソッドの呼び出しで表現する。
        /// </summary>
        public async UniTask ShowCharacter(CharacterPosition position, Sprite sprite, CancellationToken token)
        {
            var image = GetCharacterImage(position);
            if (image == null)
            {
                HideCharacter(position);
                return;
            }

            image.sprite = sprite;
            image.SetImageAlpha(0f);
            image.gameObject.SetActive(true);

            var rectTransform = image.rectTransform;
            
            Tween tween;
            switch (position)
            {
                case CharacterPosition.Left:
                    rectTransform.anchoredPosition += new Vector2(100, 0f);
                    tween = rectTransform.DOAnchorPosX(rectTransform.anchoredPosition.x - 100f, FadeDuration);
                    break;
                case CharacterPosition.Right:
                    rectTransform.anchoredPosition += new Vector2(-100, 0f);
                    tween = rectTransform.DOAnchorPosX(rectTransform.anchoredPosition.x + 100f, FadeDuration);
                    break;
                default:
                    throw new InvalidOperationException("無効なキャラクター位置です: " + position);
            }
            
            await UniTask.WhenAll(
                image.DOFade(1f, FadeDuration).ToUniTask(),
                tween.ToUniTask()
                );
        }

        /// <summary>
        /// 指定した位置のキャラクター表示を消す。
        /// </summary>
        public void HideCharacter(CharacterPosition position)
        {
            var image = GetCharacterImage(position);
            if (image == null) return;

            image.gameObject.SetActive(false);
        }

        /// <summary>
        /// 指定した位置のキャラクターを明るく、それ以外の位置のキャラクターを半分暗く表示する。
        /// セリフの発話者・表情変更の対象を目立たせるためのハイライト表現。
        /// </summary>
        public void HighlightCharacter(CharacterPosition position)
        {
            SetBrightness(CharacterPosition.Left, position == CharacterPosition.Left ? HighlightedBrightness : DimmedBrightness);
            SetBrightness(CharacterPosition.Right, position == CharacterPosition.Right ? HighlightedBrightness : DimmedBrightness);
        }

        private void SetBrightness(CharacterPosition position, float brightness)
        {
            var image = GetCharacterImage(position);
            if (image == null) return;

            image.SetImageBrightness(brightness);
        }

        private Image GetCharacterImage(CharacterPosition position)
        {
            return position switch
            {
                CharacterPosition.Left => _leftCharacterImage,
                CharacterPosition.Right => _rightCharacterImage,
                _ => null
            };
        }
    }
}
