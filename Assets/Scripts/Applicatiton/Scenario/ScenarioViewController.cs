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

        // CharacterExpressionChangeEvent は表示位置を持たないため、
        // 直近の CharacterAppearEvent から「どのキャラクターがどの位置にいるか」を覚えておく。
        private readonly Dictionary<string, CharacterPosition> _characterPositions = new();

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
            _characterPositions.Clear();

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
                    _view.ShowPhrase(phrase.CharaText, phrase.Text, destroyCancellationToken);
                    break;

                case CharacterAppearEvent appear:
                    if (appear.Character　!= null) _characterPositions[appear.Character.CharacterId] = appear.Position;
                    await _view.ShowCharacter(appear.Position, appear.Character ? appear.Character.GetSprite(appear.Emotion) : null, destroyCancellationToken);
                    break;

                case CharacterExpressionChangeEvent expression:
                    if (_characterPositions.TryGetValue(expression.Character.CharacterId, out var position))
                    {
                        await _view.ShowCharacter(position, expression.Character.GetSprite(expression.Emotion), destroyCancellationToken);
                    }
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
