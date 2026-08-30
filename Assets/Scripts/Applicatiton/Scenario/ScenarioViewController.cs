using System;
using System.Collections.Generic;
using System.Threading;
using Cysharp.Threading.Tasks;
using Domain.Scenario;
using Domain.Scenario.Controller;
using UnityEngine;

namespace Applicatiton.Scenario
{
    /// <summary>
    /// ビューにモデルの状態を反映させるコントローラークラス。
    /// <see cref="ScenarioViewModel"/> の状態を監視し、状態が変化した場合に <see cref="ScenarioView"/> に通知する。
    /// シナリオイベントの種類に応じた表示内容の判断（分岐）はこのクラスが担い、View は渡された値を表示するだけにする。
    /// </summary>
    public class ScenarioViewController : MonoBehaviour
    {
        // シナリオ終了時に BGM をフェードアウトさせる時間（秒）。
        private const float BgmFadeOutDurationSeconds = 0.5f;

        // BgmEvent でのフェードイン・フェードアウトそれぞれの時間（秒）。
        private const float BgmEventFadeDurationSeconds = 1.0f;

        [SerializeField] private ScenarioView _view;

        // 各位置に現在どの CharacterData がいるかを覚えておく。
        // Phrase・CharacterExpressionChangeEvent は位置だけを指定するため、
        // ここから実際の CharacterData を引いて参照する。
        private CharacterData _leftCharacter;
        private CharacterData _rightCharacter;

        // これまでに表示したセリフのログ。ログ表示 UI から参照される。
        private readonly List<ScenarioLogEntry> _log = new();
        public IReadOnlyList<ScenarioLogEntry> Log => _log;

        /// <summary>
        /// <see cref="Log"/> の内容が更新された際に発火する。
        /// </summary>
        public event Action LogUpdated;

        private ScenarioViewModel _viewModel;

        /// <summary>
        /// シナリオ画面全体のフェードインを行う。シナリオ起動時に呼び出す。
        /// </summary>
        public UniTask FadeInAsync(CancellationToken token) => _view.FadeInAsync(token);

        /// <summary>
        /// シナリオ画面全体のフェードアウトを行う。シナリオ終了時に呼び出す。
        /// </summary>
        public UniTask FadeOutAsync(CancellationToken token) => _view.FadeOutAsync(token);

        /// <summary>
        /// シナリオ終了時に BGM をフェードアウトさせながら停止する。
        /// BGM が再生されていない、または AudioManager が存在しない場合は何もしない。
        /// </summary>
        public UniTask FadeBgmOutAsync(CancellationToken token)
        {
            return AudioManager.Instance != null
                ? AudioManager.Instance.FadeBGMAsync(BgmFadeOutDurationSeconds, token)
                : UniTask.CompletedTask;
        }

        /// <summary>
        /// 早送り・スキップ中かどうかに応じて、演出の速度を切り替える。
        /// </summary>
        public void SetFastForward(bool enabled) => _view.SetFastForward(enabled);

        /// <summary>
        /// 監視対象の <see cref="ScenarioViewModel"/> を登録し、状態変化の購読を開始する。
        /// </summary>
        public void Initialize(ScenarioViewModel viewModel)
        {
            if (_viewModel != null)
            {
                _viewModel.CurrentEventChanged -= OnCurrentEventChanged;
                _viewModel.BackgroundChanged -= OnBackgroundChanged;
            }

            _viewModel = viewModel;
            _leftCharacter = null;
            _rightCharacter = null;
            _log.Clear();
            LogUpdated?.Invoke();

            _viewModel.CurrentEventChanged += OnCurrentEventChanged;
            _viewModel.BackgroundChanged += OnBackgroundChanged;

            if (_viewModel.CurrentEvents != null)
            {
                OnCurrentEventChanged(_viewModel.CurrentEvents);
            }
        }

        /// <summary>
        /// 同じタイミングで実行するシナリオイベント群を受け取り、すべて同時に実行する。
        /// </summary>
        private void OnCurrentEventChanged(List<IScenarioEvent> scenarioEvents)
        {
            HandleEventsAsync(scenarioEvents).Forget();
        }

        /// <summary>
        /// シナリオ起動時に背景が割り当てられている場合、ビューの背景を変更する。
        /// </summary>
        private void OnBackgroundChanged(Sprite background)
        {
            _view.SetBackground(background);
        }

        private async UniTask HandleEventsAsync(List<IScenarioEvent> scenarioEvents)
        {
            if (scenarioEvents == null) return;

            var tasks = new List<UniTask>(scenarioEvents.Count);
            foreach (var scenarioEvent in scenarioEvents)
            {
                tasks.Add(HandleEventAsync(scenarioEvent));
            }

            await UniTask.WhenAll(tasks);
        }

        private async UniTask HandleEventAsync(IScenarioEvent scenarioEvent)
        {
            switch (scenarioEvent)
            {
                case BackgroundChangeEvent background:
                    if (background.Background != null)
                    {
                        await _view.ChangeBackgroundAsync(background.Background, destroyCancellationToken);
                    }
                    break;

                case Phrase phrase:
                    var speakerName = GetCharacter(phrase.CharaPosition)?.DisplayName ?? string.Empty;
                    await _view.ShowPhrase(speakerName, phrase.Text, destroyCancellationToken);
                    _view.HighlightCharacter(phrase.CharaPosition);
                    _log.Add(new ScenarioLogEntry(speakerName, phrase.Text));
                    LogUpdated?.Invoke();
                    break;

                case CharacterAppearEvent appear:
                    SetCharacter(appear.Position, appear.Character);
                    await _view.ShowCharacter(appear.Position, appear.Character != null ? appear.Character.GetSprite(appear.Emotion) : null, destroyCancellationToken);
                    break;

                case CharacterExpressionChangeEvent expression:
                    var character = GetCharacter(expression.Position);
                    if (character != null)
                    {
                        await _view.ShowCharacter(expression.Position, character.GetSprite(expression.Emotion), destroyCancellationToken);
                    }
                    _view.HighlightCharacter(expression.Position);
                    break;

                case SePlayEvent se:
                    AudioManager.Instance?.PlaySe(se.Clip);
                    break;

                case BgmEvent bgm:
                    await HandleBgmEventAsync(bgm);
                    break;
            }
        }

        /// <summary>
        /// BGM の再生・停止を行う。
        /// 再生時に既に BGM が再生中の場合は、シナリオ側の停止漏れとして例外を投げる。
        /// </summary>
        private async UniTask HandleBgmEventAsync(BgmEvent bgmEvent)
        {
            if (AudioManager.Instance == null) return;

            switch (bgmEvent.Action)
            {
                case BgmEventAction.Play:
                    if (AudioManager.Instance.IsBgmPlaying)
                    {
                        throw new InvalidOperationException(
                            "BgmEvent: BGM が既に再生中です。再生する前に停止するイベントを挟んでください。");
                    }

                    if (bgmEvent.Bgm == null) break;

                    if (bgmEvent.IsFadeIn)
                    {
                        await AudioManager.Instance.PlayBgmWithFadeInAsync(
                            bgmEvent.Bgm, bgmEvent.IsLoop, BgmEventFadeDurationSeconds, destroyCancellationToken);
                    }
                    else
                    {
                        AudioManager.Instance.PlayBgm(bgmEvent.Bgm, bgmEvent.IsLoop);
                    }

                    _viewModel.NotifyBgmStarted();
                    break;

                case BgmEventAction.Stop:
                    if (bgmEvent.IsFadeOut)
                    {
                        await AudioManager.Instance.FadeBGMAsync(BgmEventFadeDurationSeconds, destroyCancellationToken);
                    }
                    else
                    {
                        AudioManager.Instance.StopBgm();
                    }
                    break;
            }
        }

        /// <summary>
        /// 指定した位置に現在いる <see cref="CharacterData"/> を取得する。まだ誰もいなければ null。
        /// </summary>
        private CharacterData GetCharacter(CharacterPosition position)
        {
            return position switch
            {
                CharacterPosition.Left => _leftCharacter,
                CharacterPosition.Right => _rightCharacter,
                _ => null
            };
        }

        /// <summary>
        /// 指定した位置にいる <see cref="CharacterData"/> を更新する。
        /// </summary>
        private void SetCharacter(CharacterPosition position, CharacterData character)
        {
            switch (position)
            {
                case CharacterPosition.Left:
                    _leftCharacter = character;
                    break;
                case CharacterPosition.Right:
                    _rightCharacter = character;
                    break;
            }
        }

        private void OnDestroy()
        {
            if (_viewModel != null)
            {
                _viewModel.CurrentEventChanged -= OnCurrentEventChanged;
                _viewModel.BackgroundChanged -= OnBackgroundChanged;
            }
        }
    }
}
