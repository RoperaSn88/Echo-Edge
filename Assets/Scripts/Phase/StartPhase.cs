using Cysharp.Threading.Tasks;
using UI;
using UnityEngine;

public class StartPhase : IPhase
{
    private static StartPhase _instance;
    public static StartPhase Instance => _instance ??= new StartPhase();

    public async UniTask<IPhase> WaitPhase()
    {
        // 1. PlayerStatusPresenterからプレイヤーのデータを取得してBattleManagerにセット
        await UniTask.WaitUntil(() => PlayerStatusPresenter.Instance != null);
        var status = PlayerStatusPresenter.Instance.PlayerBattleStatus;
        BattleManager.RegisterPlayer(PlayerStatusPresenter.Instance.PlayerBattleStatus);
        
        PlayerStatusPresenter.Instance.SetPlayerHP(status.HP, status.MaxHP);
        var energyResult = EnergyManager.AddEnergy(0);
        PlayerStatusPresenter.Instance.SetEnergy(energyResult.gaugeValue, energyResult.energyCount);

        // 2. 敵や壁の配置が完了するまでawait
        await UniTask.WaitUntil(() => MapManager.Instance != null);
        await MapManager.Instance.BuildStageFromCsv();

        // 敵の数をゲームクリアマネージャーにキャッシュする
        GameClearManager.SetEnemyCount(MapManager.Instance.CountEnemies());

        // 3. Panelをフェードイン
        await AudioManager.Instance.PlayBgm(BgmAudioType.Battle, true);
        await UIPresenter.Instance.FadeInAsync();
        
        var isTutorial = PlayerPrefs.GetInt("TutorialCompleted", 0);
        if (isTutorial == 0)
        {
            PlayerPrefs.SetInt("TutorialCompleted", 1);
            PlayerPrefs.Save();
            await TutorialActivator.Instance.StartTutorial();
        }
        Debug.Log(BattleManager.PlayerStatus);

        return PlayerPhase.Instance;
    }
}
