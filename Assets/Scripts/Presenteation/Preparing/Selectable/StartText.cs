using UnityEngine;
using System.Threading;
using Cysharp.Threading.Tasks;
using DG.Tweening;
using UnityEngine.UI;

using EchoEdge.App.Preparing;
using EchoEdge.App.Scenario;
using EchoEdge.App.Scene;
using EchoEdge.Domain.Preparing;
using EchoEdge.Domain.Scenario;
using EchoEdge.Domain.Scene;
using EchoEdge.Infra.Audio;

namespace EchoEdge.Presenter.Preparing
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
            _panel.gameObject.SetActive(true);
            var panelColor = _panel.color;
            panelColor.a = 0f;
            _panel.color = panelColor;

            await UniTask.WhenAll(
                PreparingCameraController.Instance.MoveRight(),
                _panel.DOFade(1f, _fadeDuration).ToUniTask(),
                AudioManager.Instance.FadeBGMAsync(_fadeDuration, CancellationToken.None)
            );

            // 選択中ステージがシナリオ再生対象かどうかを SelectManager が保持する設定から判定する。
            // 対象なら Scenario シーンを読み込んで再生し、見終わってからメインゲームへ移行する。
            // 対象外（または設定未割り当て）なら Scenario シーンをロードせず、そのまま即メインゲームへ移行する。
            var settings = SelectManager.Instance.StageScenarioPlaybackSettings;
            if (settings == null)
            {
                Debug.LogWarning(
                    "SelectManager に StageScenarioPlaybackSettings が割り当てられていないため、シナリオを再生します");
            }

            var scenarioLoaded = false;
            if (settings == null || settings.ShouldPlayScenario(StageData.Level))
            {
                scenarioLoaded = await ScenarioStageLoader.PlayCurrentBeforeStageScenarioAsync();
            }

            await SceneLoader.AdditiveLoadAsync(GameScene.MainGame);
            if (scenarioLoaded)
            {
                SceneLoader.Unload(GameScene.Scenario);
            }
            SceneLoader.Unload(GameScene.Preparing);
        }
    }
}
