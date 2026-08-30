using System;
using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using UnityEngine;
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
        /// 現在表示中の、同時に実行するシナリオイベント群。まだ何も表示していない場合は null。
        /// </summary>
        public List<IScenarioEvent> CurrentEvents { get; private set; }

        /// <summary>
        /// シナリオを最後まで表示し終えたか。
        /// </summary>
        public bool IsFinished { get; private set; }

        /// <summary>
        /// <see cref="CurrentEvents"/> が変化した際に発火する。
        /// </summary>
        public event Action<List<IScenarioEvent>> CurrentEventChanged;

        /// <summary>
        /// シナリオの表示が最後まで完了した際に発火する。
        /// </summary>
        public event Action Finished;

        /// <summary>
        /// シナリオ起動時に背景が変更された際に発火する。
        /// </summary>
        public event Action<Sprite> BackgroundChanged;

        /// <summary>
        /// このシナリオが BGM の再生を開始したかどうか。
        /// シナリオ終了時に BGM をフェードアウトすべきかの判断に使う
        /// （このシナリオが BGM を割り当てていない場合、既存の BGM を止めないようにするため）。
        /// </summary>
        public bool HasStartedBgm { get; private set; }

        /// <summary>
        /// Addressables から <see cref="ScenarioData"/> を読み込み、先頭のページから再生できる状態にする。
        /// </summary>
        /// <param name="address">読み込む ScenarioData の Addressable アドレス。</param>
        public async UniTask OnInitializeAsync(string address)
        {
            _currentIndex = -1;
            CurrentEvents = null;
            IsFinished = false;
            HasStartedBgm = false;

            try
            {
                _scenarioData = await Addressables.LoadAssetAsync<ScenarioData>(address);
            }
            catch (Exception e)
            {
                Debug.LogError($"シナリオデータの読み込みに失敗しました (address: {address}): {e}");
                _scenarioData = null;
            }

            if (_scenarioData == null || _scenarioData.Events.Count == 0)
            {
                IsFinished = true;
                Finished?.Invoke();
                return;
            }

            PlayBgmIfAssigned();
            ApplyStartupBackgroundIfAssigned();

            await ShowNext();
        }

        /// <summary>
        /// シナリオに BGM が割り当てられている場合、最初の Step が再生される前にループ再生を開始する。
        /// </summary>
        private void PlayBgmIfAssigned()
        {
            var bgm = _scenarioData.Bgm;
            if (bgm == null) return;

            if (AudioManager.Instance == null)
            {
                Debug.LogWarning("AudioManager.Instance が見つからないため、シナリオの BGM を再生できません。");
                return;
            }

            AudioManager.Instance.PlayBgm(bgm, true);
            HasStartedBgm = true;
        }

        /// <summary>
        /// シナリオに背景が割り当てられている場合、最初の Step が再生される前に背景を変更する。
        /// </summary>
        private void ApplyStartupBackgroundIfAssigned()
        {
            var background = _scenarioData.Background;
            if (background == null) return;

            BackgroundChanged?.Invoke(background);
        }

        /// <summary>
        /// 保持している <see cref="ScenarioData"/> の次のページ（同時に実行するシナリオイベント群）を表示する。
        /// 最後まで到達している場合は <see cref="IsFinished"/> を true にして <see cref="Finished"/> を発火する。
        /// </summary>
        public async UniTask ShowNext()
        {
            if (_scenarioData == null || IsFinished) return;

            var rows = _scenarioData.Events;
            var nextIndex = _currentIndex + 1;

            if (nextIndex >= rows.Count)
            {
                IsFinished = true;
                Finished?.Invoke();
                return;
            }

            _currentIndex = nextIndex;
            CurrentEvents = rows[_currentIndex];
            CurrentEventChanged?.Invoke(CurrentEvents);
        }
    }
}
