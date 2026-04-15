using System;
using Cysharp.Threading.Tasks;
using UnityEngine;

public class UnitAction : IUnitAction
{
    private BattleStatus _battleStatus;
    private BaseUnitView _view;

    public UnitAction(BattleStatus battleStatus, BaseUnitView view)
    {
        _battleStatus = battleStatus;
        _view = view;
    }

    public async UniTask Attack()
    {
        BattleManager.RegisterEnemy(_battleStatus);
        await _view.WaitAttack();
        Time.timeScale = 0.001f;
        var damageValue = await BattleManager.PlayerDamage();
        Time.timeScale = 1.0f;

        UIPresenter.Instance.AppearDamageText($"{damageValue.damage}", PlayerController.Instance.transform.position).Forget();

        await UniTask.Delay(TimeSpan.FromSeconds(1.0f));

        BattleManager.ResetQTE();
        await CameraManager.Instance.ActResetCameraTarget();
    }

    public void Dead()
    {
        // 死亡時と同時に発生する処理
    }

    public async UniTask Specific()
    {

    }
}
