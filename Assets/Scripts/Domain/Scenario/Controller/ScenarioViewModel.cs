using System;
using Cysharp.Threading.Tasks;
using UnityEngine.AddressableAssets;

namespace Domain.Scenario.Controller
{
    /// <summary>
    /// シナリオの再生状態を管理するモデルクラス。
    /// <see cref="ScenarioData"/> を保持し、次のページの表示や Addressables からの読み込みを行う。
    /// </summary>
    public class ScenarioViewModel
    {
        private ScenarioData _scenarioData;
        private int _currentIndex = -1;

        /// <summary>
        /// 現在表示中のシナリオイベント。まだ何も表示していない場合は null。
        /// </summary>
        public IScenarioEvent CurrentEvent { get; private set; }

        /// <summary>
        /// シナリオを最後まで表示し終えたか。
        /// </summary>
        public bool IsFinished { get; private set; }

        /// <summary>
        /// <see cref="CurrentEvent"/> が変化した際に発火する。
        /// </summary>
        public event Action<IScenarioEvent> CurrentEventChanged;

        /// <summary>
        /// シナリオの表示が最後まで完了した際に発火する。
        /// </summary>
        public event Action Finished;

        /// <summary>
        /// Addressables から <see cref="ScenarioData"/> を読み込み、先頭のページから再生できる状態にする。
        /// </summary>
        /// <param name="address">読み込む ScenarioData の Addressable アドレス。</param>
        public async UniTask OnInitializeAsync(string address)
        {
            _scenarioData = await Addressables.LoadAssetAsync<ScenarioData>(address);
            _currentIndex = -1;
            CurrentEvent = null;
            IsFinished = false;

            ShowNext();
        }

        /// <summary>
        /// 保持している <see cref="ScenarioData"/> の次のページ（シナリオイベント）を表示する。
        /// 最後まで到達している場合は <see cref="IsFinished"/> を true にして <see cref="Finished"/> を発火する。
        /// </summary>
        public async UniTask ShowNext()
        {
            if (_scenarioData == null || IsFinished) return;

            var events = _scenarioData.Events;
            var nextIndex = _currentIndex + 1;

            if (nextIndex >= events.Count)
            {
                IsFinished = true;
                Finished?.Invoke();
                return;
            }

            _currentIndex = nextIndex;
            CurrentEvent = events[_currentIndex];
            CurrentEventChanged?.Invoke(CurrentEvent);
        }
    }
}
