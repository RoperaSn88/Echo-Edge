using Cysharp.Threading.Tasks;
using UnityEngine;

using EchoEdge.App.Battle;
using EchoEdge.App.PlayerData;
using EchoEdge.Domain.Battle;
using EchoEdge.Domain.UI;
using EchoEdge.Infra.Tutorial;
using EchoEdge.Presenter.UI;

namespace EchoEdge.Domain.Phase
{
    /// <summary>
    /// メインゲーム開始時の操作フェーズ
    /// </summary>
    public class StartPhase : IPhase
    {
        private static StartPhase _instance;
        public static StartPhase Instance => _instance ??= new StartPhase();

        public async UniTask<IPhase> WaitPhase()
        {
            // ドメインイベントハンドラーをリセットして登録する
            DomainEventDispatcher.Clear();

            // 1. PlayerStatusPresenterからプレイヤーのデータを取得してBattleManagerにセット

            await UniTask.WaitUntil(() => PlayerStatusPresenter.Instance != null);
            var status = PlayerStatusRegistar.SetPlayerStatus();
            status.Initialize();
            BattleManager.RegisterPlayer(status);

            PlayerStatusPresenter.Instance.SetPlayerHP(status.HP, status.MaxHP);

            // エナジーをリセットしてから表示する
            EnergyManager.Reset();
            var energyResult = EnergyManager.AddEnergy(0);
            PlayerStatusPresenter.Instance.SetEnergy(energyResult.gaugeValue, energyResult.energyCount);

            // オブジェクトプールをリセットする
            EnergyWallManager.Reset();
            BattleManager.ResetQTE();
            BattleManager.ResetCombo();
            GameReward.ResetStageExperience();

            // 2. ウェーブ定義を読み込み、0番目のウェーブから敵や壁の配置が完了するまでawait
            // (クリア条件種別は各ウェーブのCSV1行目から MapManager.BuildStageFromCsv 内で設定される)
            await UniTask.WaitUntil(() => MapManager.Instance != null);
            await WaveManager.LoadStageWavesAsync();
            await MapManager.Instance.BuildStageFromCsv();

            // 3. Panelをフェードイン（最初のウェーブがボス戦ならボスBGMを流す）
            await BattleBgmController.PlayStageStartBgmAsync();
            await UIPresenter.Instance.FadeInAsync();

            // 4. チュートリアルが未完了の場合はチュートリアルを開始する
            if (TutorialSaveManager.TryBeginTutorial(TutorialKinds.FirstBattle))
            {
                try
                {
                    await TutorialActivator.Instance.StartTutorial(TutorialKinds.FirstBattle);
                }catch (System.Exception e)
                {
                    Debug.LogWarning($"チュートリアルを中止: {e}");
                }
            }
            
            await TurnChangeView.Instance.ShowTurnChange(TurnChangeKinds.PlayerTurn);
            
            return PlayerPhase.Instance;
        }
    }
}
