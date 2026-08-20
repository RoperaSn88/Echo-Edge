using System;
using System.Threading;
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
        private const float CharacterMoveOffsetX = 40f;

        private const float TextMotionDuration = 0.25f;
        private const float TextMoveOffsetX = -30f;

        private const float ScreenFadeDuration = 0.4f;

        private const float HighlightedBrightness = 1f;
        private const float DimmedBrightness = 0.5f;

        private Vector2 _speakerTextHomePosition;
        private Vector2 _bodyTextHomePosition;
        private Vector2 _leftCharacterHomePosition;
        private Vector2 _rightCharacterHomePosition;

        private void Awake()
        {
            _speakerTextHomePosition = _speakerText.rectTransform.anchoredPosition;
            _bodyTextHomePosition = _bodyText.rectTransform.anchoredPosition;
            _leftCharacterHomePosition = _leftCharacterImage.rectTransform.anchoredPosition;
            _rightCharacterHomePosition = _rightCharacterImage.rectTransform.anchoredPosition;
        }

        /// <summary>
        /// シナリオ画面全体を覆うパネルをフェードインさせ、画面を表示する。
        /// シナリオ起動時に呼び出す。
        /// </summary>
        public UniTask FadeInAsync(CancellationToken token)
        {
            _fadeImage.DOKill();
            return _fadeImage.DOFade(0f, ScreenFadeDuration).ToUniTask(cancellationToken: token);
        }

        /// <summary>
        /// シナリオ画面全体を覆うパネルをフェードアウトさせ、画面を隠す。
        /// シナリオ終了時に呼び出す。
        /// </summary>
        public UniTask FadeOutAsync(CancellationToken token)
        {
            _fadeImage.DOKill();
            return _fadeImage.DOFade(1f, ScreenFadeDuration).ToUniTask(cancellationToken: token);
        }

        /// <summary>
        /// セリフ（話者名と本文）を表示する。
        /// 表示開始時に左へ少しオフセットした位置・透明度0から、元の位置・透明度1へ移動するモーションを行う。
        /// </summary>
        public UniTask ShowPhrase(string speakerName, string body, CancellationToken token)
        {
            _speakerText.text = speakerName;
            _bodyText.text = body;

            return UniTask.WhenAll(
                AnimateTextAppear(_speakerText, _speakerTextHomePosition, token),
                AnimateTextAppear(_bodyText, _bodyTextHomePosition, token)
            );
        }

        private static UniTask AnimateTextAppear(TMP_Text text, Vector2 homePosition, CancellationToken token)
        {
            var rectTransform = text.rectTransform;
            rectTransform.DOKill();
            text.DOKill();

            rectTransform.anchoredPosition = homePosition + new Vector2(TextMoveOffsetX, 0f);
            var color = text.color;
            color.a = 0f;
            text.color = color;

            return UniTask.WhenAll(
                rectTransform.DOAnchorPos(homePosition, TextMotionDuration).SetEase(Ease.OutQuad).ToUniTask(cancellationToken: token),
                text.DOFade(1f, TextMotionDuration).ToUniTask(cancellationToken: token)
            );
        }

        /// <summary>
        /// 指定した位置にキャラクターのスプライトを表示する。
        /// キャラクター登場・表情変更のいずれも、このメソッドの呼び出しで表現する。
        /// 画面中央側へ少しオフセットした位置・透明度0から、元の位置・透明度1へ移動するモーションを行う。
        /// </summary>
        public UniTask ShowCharacter(CharacterPosition position, Sprite sprite, CancellationToken token)
        {
            var image = GetCharacterImage(position);
            if (image == null)
            {
                HideCharacter(position);
                return UniTask.CompletedTask;
            }

            var rectTransform = image.rectTransform;
            rectTransform.DOKill();
            image.DOKill();

            var homePosition = GetCharacterHomePosition(position);
            var offsetX = position == CharacterPosition.Left ? CharacterMoveOffsetX : -CharacterMoveOffsetX;

            Tweener fadeTween = null;
            Tweener moveTween = null;
            
            if (sprite != null)
            {
                image.sprite = sprite;
                image.SetImageAlpha(0f);
                image.gameObject.SetActive(true);
                rectTransform.anchoredPosition = homePosition + new Vector2(offsetX, 0f);
                fadeTween = image.DOFade(1f, FadeDuration);
                moveTween = rectTransform.DOAnchorPos(homePosition, FadeDuration).SetEase(Ease.OutQuad);
            }
            else
            {
                image.SetImageAlpha(1f);
                image.gameObject.SetActive(true);
                fadeTween = image.DOFade(0f, FadeDuration);
                moveTween = rectTransform.DOAnchorPos(homePosition + new Vector2(offsetX, 0), FadeDuration).SetEase(Ease.OutQuad);
            }

            return UniTask.WhenAll(
                fadeTween.ToUniTask(cancellationToken: token),
                moveTween.ToUniTask(cancellationToken: token)
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

        private Vector2 GetCharacterHomePosition(CharacterPosition position)
        {
            return position switch
            {
                CharacterPosition.Left => _leftCharacterHomePosition,
                CharacterPosition.Right => _rightCharacterHomePosition,
                _ => throw new InvalidOperationException("無効なキャラクター位置です: " + position)
            };
        }
    }
}
