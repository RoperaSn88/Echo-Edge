using Cysharp.Threading.Tasks;
using UI;
using UnityEngine;

public class StartPhase : IPhase
{
    private static StartPhase _instance;
    public static StartPhase Instance => _instance ??= new StartPhase();
    public const string TutorialCompletedKey = "TutorialCompleted";

    public async UniTask<IPhase> WaitPhase()
    {
        // 1. PlayerStatusPresenterからプレイヤーのデータを取得してBattleManagerにセット
        await UniTask.WaitUntil(() => PlayerStatusPresenter.Instance != null);
        var status = PlayerStatusPresenter.Instance.PlayerBattleStatus;
        status.Initialize();
        BattleManager.RegisterPlayer(status);
        
        PlayerStatusPresenter.Instance.SetPlayerHP(status.HP, status.MaxHP);
        Debug.Log("Move: " + status.Move);

        // エナジーをリセットしてから表示する
        EnergyManager.Reset();
        var energyResult = EnergyManager.AddEnergy(0);
        PlayerStatusPresenter.Instance.SetEnergy(energyResult.gaugeValue, energyResult.energyCount);

        // オブジェクトプールをリセットする
        EnergyWallManager.Reset();
        BattleManager.ResetQTE();
        BattleManager.ResetCombo();

        // 2. 敵や壁の配置が完了するまでawait
        await UniTask.WaitUntil(() => MapManager.Instance != null);
        await MapManager.Instance.BuildStageFromCsv();

        // 3. Panelをフェードイン
        if (AudioManager.Instance)
        {
            await AudioManager.Instance.PlayBgm(BgmAudioType.Battle, true);
        }
        await UIPresenter.Instance.FadeInAsync();
        
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
