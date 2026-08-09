using Applicatiton.Battle.Phase;
using Cysharp.Threading.Tasks;
using UI;
using UnityEngine;

/// <summary>
/// メインゲーム開始時の操作フェーズ
/// </summary>
public class StartPhase : IPhase
{
    private static StartPhase _instance;
    public static StartPhase Instance => _instance ??= new StartPhase();
    public const string TutorialCompletedKey = "TutorialCompleted";
    
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

        // 3. Panelをフェードイン
        if (AudioManager.Instance)
        {
            await AudioManager.Instance.PlayBgm(BgmAudioType.Battle, true);
        }
        await UIPresenter.Instance.FadeInAsync();
        
        // 4. チュートリアルが未完了の場合はチュートリアルを開始する
        var isTutorial = PlayerPrefs.GetInt(TutorialCompletedKey, 0);
        if (isTutorial == 0)
        {
            PlayerPrefs.SetInt(TutorialCompletedKey, 1);
            PlayerPrefs.Save();
            try
            {
                await TutorialActivator.Instance.StartTutorial();
            }catch (System.Exception)
            {
                Debug.Log("チュートリアルを中止");
            }
        }

        return PlayerPhase.Instance;
    }
}
