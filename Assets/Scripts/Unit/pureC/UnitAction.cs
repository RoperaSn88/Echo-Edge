using System;
using Cysharp.Threading.Tasks;
using DG.Tweening;
using UnityEngine;
using UnityEngine.UI;

public class UnitAction : IUnitAction
{
    private BattleStatus _battleStatus;
    private BaseUnitView _view;
    private Image _image;

    public UnitAction(BattleStatus battleStatus, BaseUnitView view, Image image)
    {
        _battleStatus = battleStatus;
        _view = view;
        _image = image;
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
        if (_image != null)
        {
            _image.DOFade(0f, 1f);
        }
    }
}
