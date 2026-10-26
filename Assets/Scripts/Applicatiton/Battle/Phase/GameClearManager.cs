using System;
using System.Threading;
using Cysharp.Threading.Tasks;
using UnityEngine;

using EchoEdge.App.Preparing;
using EchoEdge.App.Scenario;
using EchoEdge.App.Scene;
using EchoEdge.Domain.Phase;
using EchoEdge.Domain.Preparing;
using EchoEdge.Domain.Scenario;
using EchoEdge.Domain.Scene;
using EchoEdge.Infra.Audio;
using EchoEdge.Infra.Camera;
using EchoEdge.Presenter.Player;
using EchoEdge.Presenter.UI;

namespace EchoEdge.App.Battle
{
    /// <summary>
    /// ゲームクリアの演出と、クリア後の「続ける」「休む」の分岐を管理するクラス。
    /// </summary>
    public static class GameClearManager
    {
        private static IStageClearTask _currentTask;

        /// <summary>
        /// 現在のウェーブのクリア条件の種類
        /// </summary>
        public static StageClearConditionType CurrentConditionType { get; private set; }
        private static bool _isClear;
        public static bool IsClear => _isClear;

        private static bool _gameClearAsyncStarted;

        /// <summary>
        /// クリア演出シーケンス（暗転→報酬表示→シナリオ再生→シーン遷移）が開始済みか。
        /// これが true の間は MainGame 裏でバトルサイクルを進めてはいけない。
        /// </summary>
        public static bool IsGameClearSequenceRunning => _gameClearAsyncStarted;

        /// <summary>
        /// このステージで使うクリア条件タスクを切り替える。ステージ開始時（StartPhase）から呼ぶ。
        /// 直前のタスクの購読を解除し、新しいタスクの購読を開始する。
        /// </summary>
        public static void SetStageClearConditionType(StageClearConditionType conditionType)
        {
            _currentTask?.Unsubscribe();
            CurrentConditionType = conditionType;
            _currentTask = CreateTask(conditionType);
            _currentTask.Subscribe();
            _isClear = false;
            _gameClearAsyncStarted = false;
        }

        /// <summary>
        /// ステージ（ウェーブ）開始時に、現在アクティブなタスクへ条件の初期値を渡す。
        /// </summary>
        public static void Initialize(int conditionValue)
        {
            _currentTask?.Initialize(conditionValue);
        }

        private static IStageClearTask CreateTask(StageClearConditionType conditionType)
        {
            switch (conditionType)
            {
                case StageClearConditionType.Endurance:
                    return new EndureStageClearTask();
                case StageClearConditionType.DefeatAllEnemies:
                default:
                    return new DefeatAllEnemiesStageClearTask();
            }
        }

        /// <summary>
        /// ステージクリア条件の進捗表示を更新する。
        /// </summary>
        public static void UpdateText(string context, int value)
        {
            GameClearConditionView.Instance.RefreshText(context, value);
        }

        public static void SetStageClearCondition(bool isClear)
        {
            _isClear = isClear;
        }

        /// <summary>
        /// 新しいバトル（MainGame シーン）の開始時に、前回のクリア演出シーケンスの状態をリセットする。
        /// これを呼ばないと <see cref="IsGameClearSequenceRunning"/> が true のまま残り、
        /// 2 回目以降の PhaseManager がフェーズループへ入らず、バトルが始まらない。
        /// </summary>
        public static void ResetGameClearSequenceState()
        {
            _gameClearAsyncStarted = false;
            _isClear = false;
        }

        public static bool GameClearCondition()
        {
            if (IsClear && WaveManager.HasNextWave) return true;
            return false;
        }

        public static async UniTask StartGameClearSequenceAsync()
        {
            // ステージクリアの進行状況を記録し、次のステージを選択可能にする（ガードに関係なく実行）
            StageData.RegisterStageCleared(StageData.Level);

            if(_gameClearAsyncStarted) return;
            _gameClearAsyncStarted = true;

            // 1. 暗転する
            await UIPresenter.Instance.FadeOutAsync(0.01f);

            // 2. ステータスのUIを非表示にする
            if (PlayerStatusPresenter.Instance != null)
            {
                PlayerStatusPresenter.Instance.gameObject.SetActive(false);
            }

            // 3. プレイヤーにカメラをより大きく瞬時に拡大し、カメラの角度をy軸30ほど傾ける
            if (PlayerController.Instance != null)
            {
                CameraManager.Instance.SetGameClearCamera(PlayerController.Instance.transform.position);
            }

            // 最後に倒した敵から下1マス、横3マスに壁が存在する場合は削除する
            RemoveWallsNearLastEnemy();

            // 4. 暗転をやめて表示する
            await UIPresenter.Instance.FadeInAsync(0.01f);
            var reward = GameReward.ApplyStageClearReward();
            Time.timeScale = 0.0f;
            
            // 5. カメラを揺らす
            CameraManager.Instance.StartCameraShake();
            await GameClearRewardPresenter.Instance.ShowAsync(reward.level, reward.gainedExperience, reward.currentExperience);

            // 6. 「続ける」「休む」の選択を待つ（最終ステージでは「続ける」を出さない）
            var choice = await WaitForChoiceAsync(StageData.HasNextStage);

            // 選択時: カメラをもとの位置に戻して揺らすのをやめる
            CameraManager.Instance.StopCameraShake();
            var t1 = UIPresenter.Instance.FadeOutAsync(0.5f);
            var t2 = GameClearRewardPresenter.Instance.CloseAsync();
            var t3 = AudioManager.Instance != null
                ? AudioManager.Instance.FadeBGMAsync(0.5f, CancellationToken.None)
                : UniTask.CompletedTask;
            var t4 = AudioManager.Instance != null
                ? AudioManager.Instance.FadeAddedBGMAsync(0.5f, CancellationToken.None)
                : UniTask.CompletedTask;
            var t5 = GameClearChoicePresenter.Instance != null
                ? GameClearChoicePresenter.Instance.CloseAsync()
                : UniTask.CompletedTask;

            await UniTask.WhenAll(t1, t2, t3, t4, t5);

            Time.timeScale = 1.0f;

            EnhancementManager.AddStone(1);

            // 戻り値が true のときは Scenario シーンがロード済みなので、呼び出し側で必ずアンロードする
            // （スキップ・通常終了どちらの経路でも Scenario シーンは残るため）。
            // 再生要否は SelectManager が起動時に登録した設定を静的に参照して判定する
            // （この時点で Preparing シーンはアンロード済みのため、インスタンス参照は使えない）。
            var scenarioLoaded = false;
            if (StageScenarioPlayback.ShouldPlayScenario(StageData.Level))
            {
                scenarioLoaded = await ScenarioStageLoader.PlayCurrentAfterStageScenarioAsync();
            }

            if (choice == GameClearChoice.Continue)
            {
                await PrepareNextStageAsync(scenarioLoaded);
                return;
            }

            // 7. MainGameをアンロードし、Preparingシーンを読み込む
            await SceneLoader.AdditiveLoadAsync(GameScene.Preparing);
            if (scenarioLoaded)
            {
                SceneLoader.Unload(GameScene.Scenario);
            }
            SceneLoader.Unload(GameScene.MainGame);
        }

        /// <summary>
        /// 選択肢を表示して選ばれた行動を返す。
        /// 選択肢UIがシーンに無い場合は、従来どおりクリックを待って「休む」扱いにする。
        /// </summary>
        private static async UniTask<GameClearChoice> WaitForChoiceAsync(bool canContinue)
        {
            if (GameClearChoicePresenter.Instance != null)
            {
                return await GameClearChoicePresenter.Instance.ShowAndWaitAsync(canContinue);
            }

            Debug.LogWarning("GameClearChoicePresenter がシーンに無いため、クリックで準備シーンへ戻ります。");
            var mouseActions = new MouseClick();
            mouseActions.Enable();
            var tcs = new UniTaskCompletionSource();
            mouseActions.Mouse.MouseClick.started += _ => tcs.TrySetResult();
            await tcs.Task;
            mouseActions.Mouse.Disable();
            mouseActions.Dispose();
            return GameClearChoice.Rest;
        }

        /// <summary>
        /// 「続ける」を選んだとき: ステージナンバーを+1し、次ステージの開始前シナリオを再生してから
        /// MainGame シーンを読み込み直す。
        /// </summary>
        private static async UniTask PrepareNextStageAsync(bool afterScenarioLoaded)
        {
            // クリア済み登録（RegisterStageCleared）で次のステージは解禁済みなので、ここで +1 できる
            StageData.IncrementLevel();

            // Scenario シーンを二重にロードしないよう、クリア後シナリオのシーンを先に閉じる
            if (afterScenarioLoaded)
            {
                await SceneLoader.UnloadAsync(GameScene.Scenario);
            }

            var beforeScenarioLoaded = false;
            if (StageScenarioPlayback.ShouldPlayScenario(StageData.Level))
            {
                beforeScenarioLoaded = await ScenarioStageLoader.PlayCurrentBeforeStageScenarioAsync();
            }

            // シーンの入れ替えは待たずに走らせる。
            // この演出は敵撃破・ターン終了の処理から await されており、戻った後も実行中のフェーズ
            // （攻撃の残り、エネミーフェーズのターン表示など）がシーン内のオブジェクトや
            // static な Instance を触る。先に呼び出し元を戻し、旧 PhaseManager のループが
            // （IsGameClearSequenceRunning により）抜けきるのを待ってから入れ替える。
            ReloadMainGameAsync(beforeScenarioLoaded).Forget();
        }

        /// <summary>
        /// 旧フェーズループの終了を待つ上限。フェーズが止まったままでもクリア後に進めなくならないようにする。
        /// </summary>
        private const float PhaseLoopExitTimeoutSeconds = 10f;

        private static async UniTask ReloadMainGameAsync(bool scenarioLoaded)
        {
            var timedOut = await UniTask.WaitUntil(() => !PhaseManager.IsPhaseLoopRunning)
                .TimeoutWithoutException(TimeSpan.FromSeconds(PhaseLoopExitTimeoutSeconds), DelayType.Realtime);
            if (timedOut)
            {
                Debug.LogWarning("旧 MainGame のフェーズループが終わらないまま、シーンを読み込み直します。");
            }

            // 同じシーンを追加ロードするので、旧 MainGame のシングルトン等が残らないよう破棄を待ってから読み込む
            await SceneLoader.UnloadAsync(GameScene.MainGame);
            await SceneLoader.AdditiveLoadAsync(GameScene.MainGame);
            if (scenarioLoaded)
            {
                SceneLoader.Unload(GameScene.Scenario);
            }
        }

        private static void RemoveWallsNearLastEnemy()
        {
            if (BuildingManager.Instance == null) throw new InvalidOperationException("BuildingManager.Instance が null です。");

            // 最後に倒した敵から下1マス、横3マスの壁を削除する
            for (int offsetW = -1; offsetW <= 1; offsetW++)
            {
                BuildingManager.Instance.TryRemoveWallAt(GameReward.LastEnemyH - 1, GameReward.LastEnemyW + offsetW);
            }
        }
    }
}
