using System;
using Actions;
using Cysharp.Threading.Tasks;
using UI.Weapon;
using UnityEngine.InputSystem;

/// <summary>
/// 装備品使用フェーズ
/// </summary>
public class PlayerEquipPhase : IPhase
{
    /// <summary>
    /// 装備品使用フェーズのインスタンス
    /// </summary>
    private static PlayerEquipPhase _instance;

    /// <summary>
    /// 他のスクリプトから干渉するプロパティ
    /// </summary>
    public static PlayerEquipPhase Instance => _instance ??= new PlayerEquipPhase();

    /// <summary>
    /// クリックされたか検知するブール
    /// </summary>
    private bool _clickFlug;

    /// <summary>
    /// クリックの種類を保存する
    /// </summary>
    private ClickKinds _clickKind;

    public async UniTask<IPhase> WaitPhase()
    {
        // 装備品のUIを非表示にする
        WeaponController.Instance.HideAllWeaponUIs();

        // カメラを上方向にする
        CameraManager.Instance.ActMoveCameraToTopAngle();

        _clickFlug = false;
        PlayerActions playerActions = new PlayerActions();
        playerActions.PlayerPhase.Attack.started += OnPressAttack;
        playerActions.PlayerPhase.Skill.started += OnPressSkill;
        playerActions.Enable();

        // クリックを待つ
        await UniTask.WaitUntil(() => _clickFlug);
        _clickFlug = false;
        playerActions.Dispose();

        switch (_clickKind)
        {
            case ClickKinds.Left:
                return PlayerPhase.Instance;
            case ClickKinds.Right:
                return PlayerWeaponPhase.Instance;
        }

        throw new InvalidOperationException("クリックがうまくできない謎のエラーです");
    }

    private void OnPressAttack(InputAction.CallbackContext context)
    {
        _clickKind = ClickKinds.Left;
        _clickFlug = true;
    }

    private void OnPressSkill(InputAction.CallbackContext context)
    {
        _clickKind = ClickKinds.Right;
        _clickFlug = true;
    }
}
