using System.Threading;
using Cysharp.Threading.Tasks;
using UnityEngine.InputSystem;

namespace Domain.Scenario.Controller
{
    /// <summary>
    /// シナリオ画面における入力の判断を行うクラス。
    /// <see cref="ScenarioViewModel"/> を保持し、クリック用の InputAction を生成して
    /// クリックされるまで非同期に待機しながらページを進める。
    /// </summary>
    public class ScenarioScreenModel
    {
        private readonly ScenarioViewModel _scenarioViewModel;
        public ScenarioViewModel ScenarioViewModel => _scenarioViewModel;

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

        /// <summary>
        /// クリック用の InputAction を生成し、クリックされるまで待機してページを進める処理を
        /// シナリオが完了するまで繰り返す。呼び出し側（ビュー）の Update で毎フレーム入力を
        /// ポーリングする必要がないよう、待機はこのメソッド内で完結させる。
        /// </summary>
        /// <param name="cancellationToken">画面が閉じられた際などに待機を中断するためのトークン。</param>
        public async UniTask RunAsync(CancellationToken cancellationToken)
        {
            var mouseClick = new MouseClick();
            mouseClick.Enable();

            try
            {
                while (!_scenarioViewModel.IsFinished)
                {
                    await WaitForClickAsync(mouseClick, cancellationToken);
                    await _scenarioViewModel.ShowNext();
                }
            }
            finally
            {
                mouseClick.Mouse.Disable();
                mouseClick.Dispose();
            }
        }

        /// <summary>
        /// クリックされるまで待機する。<see cref="InputAction.started"/> のコールバックで
        /// 完了させることで、Update でのポーリングを行わずに待機を実現する。
        /// </summary>
        private static async UniTask WaitForClickAsync(MouseClick mouseClick, CancellationToken cancellationToken)
        {
            var tcs = new UniTaskCompletionSource();

            void OnClick(InputAction.CallbackContext _) => tcs.TrySetResult();

            mouseClick.Mouse.MouseClick.started += OnClick;
            try
            {
                using (cancellationToken.Register(() => tcs.TrySetCanceled(cancellationToken)))
                {
                    await tcs.Task;
                }
            }
            finally
            {
                mouseClick.Mouse.MouseClick.started -= OnClick;
            }
        }
    }
}
