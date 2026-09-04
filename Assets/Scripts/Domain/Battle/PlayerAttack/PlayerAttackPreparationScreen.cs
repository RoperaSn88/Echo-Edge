using System.Threading;
using Cysharp.Threading.Tasks;
using DG.Tweening;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace EchoEdge.Domain.Battle
{
    public class PlayerAttackPreparationScreen : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
    {
        [SerializeField]
        private PlayerAttackPreparationViewController _viewController;

        /// <summary>
        /// マウスカーソルが乗った際に適用するCanvasGroupのアルファ値。
        /// </summary>
        [SerializeField]
        private float _hoveredAlpha = 0.3f;

        /// <summary>
        /// アルファ値のフェードにかける時間（秒）。
        /// </summary>
        [SerializeField]
        private float _fadeDuration = 0.2f;

        /// <summary>
        /// シーン上のインスタンスへ他のスクリプト（<see cref="PlayerAttackPreparationPhase"/>など）から
        /// 参照するためのプロパティ。
        /// </summary>
        public static PlayerAttackPreparationScreen Instance { get; private set; }

        private readonly PlayerAttackPreparationScreenModel _screenModel = new();

        /// <summary>
        /// 攻撃準備に関する状態を保持するScreenModel。入力の切り分け（切り替え・決定の判断）は
        /// <see cref="PlayerAttackPreparationPhase"/> 側で行い、その結果をこのScreenModel経由で反映する。
        /// </summary>
        public PlayerAttackPreparationScreenModel ScreenModel => _screenModel;

        private CanvasGroup _canvasGroup;

        private void Awake()
        {
            Instance = this;

            // マウスホバーの検知にはraycastを受け取れるGraphicが必要なため、
            // 未設定の場合は透明なImageを自動で用意する。
            var image = GetComponent<Image>();
            if (image == null)
            {
                image = gameObject.AddComponent<Image>();
                image.color = new Color(0f, 0f, 0f, 0f);
            }
            image.raycastTarget = true;

            _canvasGroup = GetComponent<CanvasGroup>();
            if (_canvasGroup == null)
            {
                _canvasGroup = gameObject.AddComponent<CanvasGroup>();
            }
        }

        public async UniTask InitializeAsync(CancellationToken token)
        {
            await _screenModel.InitializeAsync();
            _viewController.Initialize(_screenModel.PlayerAttackPreparationViewModel);
            await _viewController.InitializeAsync(token);
        }

        public async UniTask OnShowAsync(CancellationToken token)
        {
            await _viewController.ShowAsync(token);
        }

        public async UniTask OnHideAsync(CancellationToken token)
        {
            await _viewController.HideAsync(token);
        }

        /// <summary>
        /// マウスカーソルがAttackInfoに乗った際に、UIの透明度を下げて背後を見やすくする。
        /// </summary>
        public void OnPointerEnter(PointerEventData eventData)
        {
            _canvasGroup.DOKill();
            _canvasGroup.DOFade(_hoveredAlpha, _fadeDuration);
        }

        /// <summary>
        /// マウスカーソルがAttackInfoから外れた際に、透明度を元に戻す。
        /// </summary>
        public void OnPointerExit(PointerEventData eventData)
        {
            _canvasGroup.DOKill();
            _canvasGroup.DOFade(1f, _fadeDuration);
        }
    }
}
