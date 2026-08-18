using System;
using System.Threading;
using Cysharp.Threading.Tasks;

namespace Domain.Scenario.Controller
{
    /// <summary>
    /// シナリオ画面における入力の判断を行うクラス。
    /// <see cref="ScenarioViewModel"/> を保持し、入力を受けてページを進めるかどうかを判断する。
    /// </summary>
    public class ScenarioScreenModel
    {
        private readonly ScenarioViewModel _scenarioViewModel;
        public ScenarioViewModel ScenarioViewModel => _scenarioViewModel;
        
        private CancellationTokenSource _cancellationTokenSource;
        
        public ScenarioScreenModel()
        {
            _scenarioViewModel = new ScenarioViewModel();
        }
        
        /// <summary>
        /// 初期化
        /// </summary>
        /// <param name="scenarioAddress"></param>
        public async UniTask InitializeAsync(string scenarioAddress)
        {
            await _scenarioViewModel.OnInitializeAsync(scenarioAddress);
        }
        
        public async UniTask NextPhraseAsync()
        {
            // IsFinishedはこのクラスが持つはず。
            if (_scenarioViewModel.IsFinished) return;
            _cancellationTokenSource?.Cancel();
            _cancellationTokenSource = new();

            try
            {
                await _scenarioViewModel.ShowNext();
            }
            catch (OperationCanceledException)
            {
                // キャンセルされた場合はフルテキスト表示
            }
            
            _scenarioViewModel.ShowNext();
        }

        public void OnCancel()
        {
            
        }
        
        /// <summary>
        /// 入力を受け取った際に呼び出す。シナリオが完了していなければ次のページへ進める。
        /// </summary>
        public void OnInputReceived()
        {
            if (_scenarioViewModel.IsFinished) return;

            _scenarioViewModel.ShowNext();
        }
    }
}
