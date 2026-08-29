using System;
using System.Threading;
using Cysharp.Threading.Tasks;
using UnityEngine.InputSystem;

namespace Domain.Scenario.Controller
{
    /// <summary>
    /// シナリオ画面における入力の判断を行うクラス。
    /// <see cref="ScenarioViewModel"/> を保持し、クリック用の InputAction を生成して
    /// クリックされるまで非同期に待機しながらページを進める。
    /// 自動再生・スキップが有効な場合は、クリックを待たずに自動でページを進める。
    /// </summary>
    public class ScenarioScreenModel
    {
        // 自動再生時、次に進むまでクリックを待つ最大時間。
        private const float AutoPlayDelaySeconds = 1.2f;

        // スキップ時、次に進むまでの待機時間。0 にはせず、僅かに待つことでフレーム分割する。
        private const float SkipAdvanceDelaySeconds = 0.05f;

        private readonly ScenarioViewModel _scenarioViewModel;
        public ScenarioViewModel ScenarioViewModel => _scenarioViewModel;

        /// <summary>
        /// 自動再生が有効かどうか。有効な場合、クリックがなくても一定時間で次に進む。
        /// </summary>
        public bool IsAutoPlayEnabled { get; private set; }

        /// <summary>
        /// スキップが有効かどうか。有効な場合、クリックを待たずに次々と進める。
        /// </summary>
        public bool IsSkipEnabled { get; private set; }

        /// <summary>
        /// 早送りが有効かどうか。進行のタイミングには影響せず、演出速度のみに影響する。
        /// </summary>
        public bool IsFastForwarding { get; private set; }

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

        public void SetAutoPlay(bool enabled) => IsAutoPlayEnabled = enabled;

        public void SetSkip(bool enabled) => IsSkipEnabled = enabled;

        public void SetFastForward(bool enabled) => IsFastForwarding = enabled;

        /// <summary>
        /// クリック用の InputAction を生成し、次に進めるまで待機してページを進める処理を
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
                    await WaitForAdvanceAsync(mouseClick, cancellationToken);
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
        /// 次のページに進めるまで待機する。
        /// スキップ中はごく短い時間の待機のみで次に進む。
        /// 自動再生中は、クリックされるか一定時間が経過するかのどちらか早い方まで待つ。
        /// どちらでもない場合は、通常通りクリックされるまで待つ。
        /// </summary>
        private async UniTask WaitForAdvanceAsync(MouseClick mouseClick, CancellationToken cancellationToken)
        {
            if (IsSkipEnabled)
            {
                await UniTask.Delay(TimeSpan.FromSeconds(SkipAdvanceDelaySeconds), cancellationToken: cancellationToken);
                return;
            }

            if (IsAutoPlayEnabled)
            {
                await UniTask.WhenAny(
                    WaitForClickAsync(mouseClick, cancellationToken),
                    UniTask.Delay(TimeSpan.FromSeconds(AutoPlayDelaySeconds), cancellationToken: cancellationToken));
                return;
            }

            await WaitForClickAsync(mouseClick, cancellationToken);
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
