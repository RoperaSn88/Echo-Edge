using Cysharp.Threading.Tasks;
using UI;
using UnityEngine;

public class StartPhase : IPhase
{
    private static StartPhase _instance;
    public static StartPhase Instance => _instance ??= new StartPhase();

    public async UniTask<IPhase> WaitPhase()
    {
        Debug.Log("StartPhase");

        // 1. PlayerStatusPresenterからプレイヤーのデータを取得してBattleManagerにセット
        await UniTask.WaitUntil(() => PlayerStatusPresenter.Instance != null);
        BattleManager.RegisterPlayer(PlayerStatusPresenter.Instance.PlayerBattleStatus);

        // 2. 敵や壁の配置が完了するまでawait
        await UniTask.WaitUntil(() => MapManager.Instance != null);
        await MapManager.Instance.BuildStageFromCsv();

        // 3. Panelをフェードイン
        await UIPresenter.Instance.FadeInAsync();

        return PlayerPhase.Instance;
    }
}
