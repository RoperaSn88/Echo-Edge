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
        private float _fadeDuration = 1.0f;

        public override async UniTask OnDecide()
        {
            if (_panel != null)
            {
                _panel.gameObject.SetActive(true);
                var panelColor = _panel.color;
                panelColor.a = 0f;
                _panel.color = panelColor;
                await _panel.DOFade(1f, _fadeDuration).ToUniTask();
            }

            SceneLoader.Load(GameScene.MainGame);
            SceneLoader.Unload(GameScene.Preparing);
        }
    }
}
