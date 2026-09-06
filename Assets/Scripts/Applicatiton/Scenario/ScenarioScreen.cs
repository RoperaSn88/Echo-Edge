using System;
using System.Threading;
using Cysharp.Threading.Tasks;
using UnityEngine;

using EchoEdge.Domain.Scenario;

namespace EchoEdge.App.Scenario
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
        [SerializeField] private ScenarioLogView _logView;

        private readonly ScenarioScreenModel _screenModel = new();

        private CancellationTokenSource _cts;

        /// <summary>
        /// 画面を初期化し、指定したシナリオデータを Addressables から読み込んで先頭ページを表示する。
        /// </summary>
        /// <param name="scenarioAddress">読み込む ScenarioData の Addressable アドレス。</param>
        public async UniTask Initialize(string scenarioAddress)
        {
            _viewController.LogUpdated -= OnLogUpdated;
            _viewController.LogUpdated += OnLogUpdated;

            _logView.VisibilityChanged -= OnLogVisibilityChanged;
            _logView.VisibilityChanged += OnLogVisibilityChanged;

            _viewController.Initialize(_screenModel.ScenarioViewModel);
            await _screenModel.InitializeAsync(scenarioAddress);
        }

        /// <summary>
        /// 自動再生の有効・無効を切り替える。有効な場合、クリックがなくても一定時間で次に進む。
        /// </summary>
        public void SetAutoPlay(bool enabled) => _screenModel.SetAutoPlay(enabled);

        /// <summary>
        /// 早送りモードを切り替える。
        /// テキストは一括表示のため、モードに応じて次の Step へ進むまでの待機時間が短くなり、
        /// 併せて演出速度も上がる。
        /// </summary>
        public void SetFastForward(FastForwardMode mode)
        {
            _screenModel.SetFastForward(mode);
            UpdatePlaybackSpeed();
        }

        /// <summary>
        /// 再生中のシナリオを最後まで待たずに中断する。
        /// 進行中のシナリオタスク（クリック待機ループ）をキャンセルしたうえで、
        /// 通常終了時と同様に画面と BGM を同時にフェードアウトし、完了してから画面を閉じる。
        /// </summary>
        public void Skip()
        {
            SkipAsync().Forget();
        }

        /// <summary>
        /// スキップ時のフェードアウト処理本体。
        /// キャンセルされた場合（連打などで <see cref="Hide"/> が先に呼ばれた場合）は何もしない。
        /// </summary>
        private async UniTaskVoid SkipAsync()
        {
            // クリック待機中のシナリオ進行ループを中断する。
            _cts?.Cancel();

            try
            {
                var screenFadeTask = _viewController.FadeOutAsync(destroyCancellationToken);
                var bgmFadeTask = _screenModel.ScenarioViewModel.HasStartedBgm
                    ? _viewController.FadeBgmOutAsync(destroyCancellationToken)
                    : UniTask.CompletedTask;

                await UniTask.WhenAll(screenFadeTask, bgmFadeTask);
            }
            catch (OperationCanceledException)
            {
                return;
            }

            Hide();
        }

        /// <summary>
        /// ログパネルの表示・非表示を切り替える。
        /// </summary>
        public void ToggleLog() => _logView.Toggle();

        /// <summary>
        /// ログパネルの表示中はクリックでシナリオが進まないようにする。
        /// </summary>
        private void OnLogVisibilityChanged(bool isOpen)
        {
            _screenModel.IsAdvanceByClickBlocked = isOpen;
        }

        /// <summary>
        /// 早送りが有効な間は、演出速度を上げる。
        /// </summary>
        private void UpdatePlaybackSpeed()
        {
            _viewController.SetFastForward(_screenModel.IsFastForwarding);
        }

        private void OnLogUpdated()
        {
            _logView.Refresh(_viewController.Log);
        }

        /// <summary>
        /// シナリオ画面を表示し、クリック待機によるページ送りを開始する。
        /// シナリオの完了を待つ必要がある場合は <see cref="ShowAndWaitForFinishAsync"/> を使用する。
        /// </summary>
        public void Show()
        {
            BeginShow();
            RunAsync(_cts.Token).Forget();
        }

        /// <summary>
        /// シナリオ画面を表示し、クリック待機によるページ送りを開始したうえで、
        /// シナリオが最後まで再生され画面が隠れるまで待機する。
        /// </summary>
        public async UniTask ShowAndWaitForFinishAsync()
        {
            BeginShow();
            await RunAndHideAsync(_cts.Token);
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
        /// 表示に必要な状態（アクティブ化・キャンセルトークンの再生成）を準備する。
        /// </summary>
        private void BeginShow()
        {
            gameObject.SetActive(true);

            _cts?.Cancel();
            _cts?.Dispose();
            _cts = new CancellationTokenSource();
        }

        /// <summary>
        /// ScreenModel の入力待機ループを実行し、シナリオが完了したら画面を隠す。
        /// </summary>
        private async UniTaskVoid RunAsync(CancellationToken cancellationToken)
        {
            await RunAndHideAsync(cancellationToken);
        }

        /// <summary>
        /// シナリオ起動時のフェードインを行ったうえで ScreenModel の入力待機ループを実行し、
        /// シナリオが完了したら画面と BGM を同時にフェードアウトしてから画面を隠す。
        /// このシナリオが BGM を再生していない場合は、既存の BGM を止めないよう BGM のフェードは行わない。
        /// キャンセルされた場合はフェードアウトを行わずに終了する。
        /// </summary>
        private async UniTask RunAndHideAsync(CancellationToken cancellationToken)
        {
            try
            {
                await _viewController.FadeInAsync(cancellationToken);
                await _screenModel.RunAsync(cancellationToken);

                var screenFadeTask = _viewController.FadeOutAsync(cancellationToken);
                var bgmFadeTask = _screenModel.ScenarioViewModel.HasStartedBgm
                    ? _viewController.FadeBgmOutAsync(cancellationToken)
                    : UniTask.CompletedTask;

                await UniTask.WhenAll(screenFadeTask, bgmFadeTask);
            }
            catch (OperationCanceledException)
            {
                return;
            }

            Hide();
        }

        private void OnDestroy()
        {
            _viewController.LogUpdated -= OnLogUpdated;
            _logView.VisibilityChanged -= OnLogVisibilityChanged;

            _cts?.Cancel();
            _cts?.Dispose();
        }
    }
}
