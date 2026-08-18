using Cysharp.Threading.Tasks;
using Domain.Scenario.Controller;
using UnityEngine;
using UnityEngine.InputSystem;

namespace Applicatiton.Scenario
{
    /// <summary>
    /// ビューコントローラーを Initialize, Show, Update するクラス。
    /// <see cref="ScenarioScreenModel"/> と <see cref="ScenarioViewController"/> を保持し、
    /// ScreenModel の状態（入力による判断結果）に応じて ViewController の表示を制御する。
    /// </summary>
    public class ScenarioScreen : MonoBehaviour
    {
        [SerializeField] private ScenarioViewController _viewController;

        private readonly ScenarioScreenModel _screenModel = new();

        /// <summary>
        /// 画面を初期化し、指定したシナリオデータを Addressables から読み込んで先頭ページを表示する。
        /// </summary>
        /// <param name="scenarioAddress">読み込む ScenarioData の Addressable アドレス。</param>
        public async UniTask Initialize(string scenarioAddress)
        {
            _viewController.Initialize(_screenModel.ScenarioViewModel);
            await _screenModel.ScenarioViewModel.OnInitializeAsync(scenarioAddress);
        }

        /// <summary>
        /// シナリオ画面を表示する。
        /// </summary>
        public void Show()
        {
            gameObject.SetActive(true);
        }

        /// <summary>
        /// シナリオ画面を隠す。
        /// </summary>
        public void Hide()
        {
            gameObject.SetActive(false);
        }

        private void Update()
        {
            if (Mouse.current != null && Mouse.current.leftButton.wasPressedThisFrame)
            {
                _screenModel.OnInputReceived();
            }

            if (_screenModel.ScenarioViewModel.IsFinished)
            {
                Hide();
            }
        }
    }
}
