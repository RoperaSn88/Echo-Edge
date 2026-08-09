using Cysharp.Threading.Tasks;
using Domain.Phase.GameClear;
using UnityEngine;

public class EnemyPhase: IPhase
{
    /// <summary>
    /// クリックされたか検知するブール
    /// </summary>
    [SerializeField]
    bool _clickFlug;

    /// <summary>
    /// エネミーフェーズのインスタンス
    /// </summary>
    private static EnemyPhase _instance;

    /// <summary>
    /// 他のスクリプトから干渉するプロパティ
    /// </summary>
    public static EnemyPhase Instance => _instance ??= new EnemyPhase();

    public async UniTask<IPhase> WaitPhase()
    {
        _clickFlug = false;
        await TurnChangeView.Instance.ShowTurnChange(TurnChangeKinds.EnemyTurn);
        EnemyPhaseStartActivator.ExecuteEnemyPhaseStartActions();
        BuildingManager.Instance?.ExecuteTurnStartActions();
        await MapManager.Instance.ExecuteTurnStartActions();
        await MapManager.Instance.MoveUnit();
        await MapManager.Instance.ExecuteTurnEndActions();
        await CameraManager.Instance.ActResetCameraTarget();

        DomainEventDispatcher.Dispatch(new TurnEndEvent());

        return PlayerPhase.Instance;
    }
}
