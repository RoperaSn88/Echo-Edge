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
        [SerializeField] private ScenarioView _view;

        // 各位置に現在どの CharacterData がいるかを覚えておく。
        // Phrase・CharacterExpressionChangeEvent は位置だけを指定するため、
        // ここから実際の CharacterData を引いて参照する。
        private CharacterData _leftCharacter;
        private CharacterData _rightCharacter;

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
        /// 監視対象の <see cref="ScenarioViewModel"/> を登録し、状態変化の購読を開始する。
        /// </summary>
        public void Initialize(ScenarioViewModel viewModel)
        {
            if (_viewModel != null)
            {
                _viewModel.CurrentEventChanged -= OnCurrentEventChanged;
            }

            _viewModel = viewModel;
            _leftCharacter = null;
            _rightCharacter = null;

            _viewModel.CurrentEventChanged += OnCurrentEventChanged;

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
                case Phrase phrase:
                    var speaker = GetCharacter(phrase.CharaPosition);
                    await _view.ShowPhrase(speaker != null ? speaker.DisplayName : string.Empty, phrase.Text, destroyCancellationToken);
                    _view.HighlightCharacter(phrase.CharaPosition);
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
            }
        }
    }
}
