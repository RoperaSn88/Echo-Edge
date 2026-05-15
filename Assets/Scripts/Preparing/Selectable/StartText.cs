using System.Threading;
using Cysharp.Threading.Tasks;
using DG.Tweening;
using UnityEngine.UI;

namespace UnityEngine.Selectable
{
    /// <summary>
    /// 選択時、パネルをフェードインしてステージをロードするテキストを管理するクラス
    /// </summary>
    public class StartText : TMPSelectObject
    {
        /// <summary>
        /// フェードに使用するパネル
        /// </summary>
        [SerializeField]
        private Image _panel;

        /// <summary>
        /// フェード時間
        /// </summary>
        [SerializeField]
        private float _fadeDuration;

        public override async UniTask OnDecide()
        {
            AudioManager.Instance.PlaySe(SeAudioType.Decide);
            _panel.gameObject.SetActive(true);
            var panelColor = _panel.color;
            panelColor.a = 0f;
            _panel.color = panelColor;

            await UniTask.WhenAll(
                PreparingCameraController.Instance.MoveRight(),
                _panel.DOFade(1f, _fadeDuration).ToUniTask(),
                AudioManager.Instance.FadeBGMAsync(_fadeDuration, CancellationToken.None)
            );

            await SceneLoader.AdditiveLoadAsync(GameScene.MainGame);
            SceneLoader.Unload(GameScene.Preparing);
        }
    }
}
