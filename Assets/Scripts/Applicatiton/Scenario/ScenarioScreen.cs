using System;
using System.Threading;
using Cysharp.Threading.Tasks;
using Domain.Scenario.Controller;
using UnityEngine;

namespace Applicatiton.Scenario
{
    /// <summary>
    /// ビューコントローラーを Initialize, Show するクラス。
    /// <see cref="ScenarioScreenModel"/> と <see cref="ScenarioViewController"/> を保持し、
    /// ScreenModel がクリック用の InputAction で入力待機を行いページを進める。
    /// Update での毎フレームポーリングは行わない。
    /// </summary>
    public class ScenarioScreen : MonoBehaviour
    {
        [SerializeField] private ScenarioViewController _viewController;

        private readonly ScenarioScreenModel _screenModel = new();

        private CancellationTokenSource _cts;

        /// <summary>
        /// 画面を初期化し、指定したシナリオデータを Addressables から読み込んで先頭ページを表示する。
        /// </summary>
        /// <param name="scenarioAddress">読み込む ScenarioData の Addressable アドレス。</param>
        public async UniTask Initialize(string scenarioAddress)
        {
            _viewController.Initialize(_screenModel.ScenarioViewModel);
            await _screenModel.InitializeAsync(scenarioAddress);
        }

        /// <summary>
        /// シナリオ画面を表示し、クリック待機によるページ送りを開始する。
        /// </summary>
        public void Show()
        {
            gameObject.SetActive(true);

            _cts?.Cancel();
            _cts?.Dispose();
            _cts = new CancellationTokenSource();
            RunAsync(_cts.Token).Forget();
        }

        /// <summary>
        /// シナリオ画面を隠す。
        /// </summary>
        public void Hide()
        {
            gameObject.SetActive(false);

            _cts?.Cancel();
            _cts?.Dispose();
            _cts = null;
        }

        /// <summary>
        /// ScreenModel の入力待機ループを実行し、シナリオが完了したら画面を隠す。
        /// </summary>
        private async UniTaskVoid RunAsync(CancellationToken cancellationToken)
        {
            try
            {
                await _screenModel.RunAsync(cancellationToken);
            }
            catch (OperationCanceledException)
            {
                return;
            }

            Hide();
        }

        private void OnDestroy()
        {
            _cts?.Cancel();
            _cts?.Dispose();
        }
    }
}
