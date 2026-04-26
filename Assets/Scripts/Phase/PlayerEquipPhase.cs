using System;
using Actions;
using Cysharp.Threading.Tasks;
using UI.Weapon;
using Unity.Mathematics;
using UnityEngine;
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

    /// <summary>
    /// クリック時のマウスのスクリーン座標
    /// </summary>
    private Vector2 _clickMousePos;

    /// <summary>
    /// クリック位置の床座標から原点(0,0)までの距離(int)
    /// </summary>
    private int _targetFloorDistance;
    public int TargetFloorDistance => _targetFloorDistance;

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

        // 左クリック時はカーソル位置の床座標を保存する
        if (_clickKind == ClickKinds.Left)
        {
            _targetFloorDistance = -1;
            Ray ray = Camera.main.ScreenPointToRay(_clickMousePos);
            if (Physics.Raycast(ray, out RaycastHit rch, math.INFINITY, PlayerAttackPhase.LayerNumber))
            {
                var xz = new Vector2(rch.point.x, rch.point.z);
                _targetFloorDistance = Mathf.RoundToInt(xz.magnitude);
            }
        }

        switch (_clickKind)
        {
            case ClickKinds.Left:
                return PlayerWeaponActivePhase.Instance;
            case ClickKinds.Right:
                return PlayerWeaponPhase.Instance;
        }

        throw new InvalidOperationException("クリックがうまくできない謎のエラーです");
    }

    private void OnPressAttack(InputAction.CallbackContext context)
    {
        _clickMousePos = CameraManager.Instance.GetMousePosition();
        _clickKind = ClickKinds.Left;
        _clickFlug = true;
    }

    private void OnPressSkill(InputAction.CallbackContext context)
    {
        _clickKind = ClickKinds.Right;
        _clickFlug = true;
    }
}
