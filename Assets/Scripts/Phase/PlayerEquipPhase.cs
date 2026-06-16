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
    /// クリック位置の床座標をマップベース座標からのオフセット(Vector2Int)で保存する
    /// </summary>
    private Vector2Int _targetFloorPos;
    public Vector2Int TargetFloorPos => _targetFloorPos;

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
        MessageManager.Instance.AppearMessage("装備品を発動するマスを左クリックしてください").Forget();

        // クリックを待つ
        await UniTask.WaitUntil(() => _clickFlug);
        _clickFlug = false;
        playerActions.Dispose();
        MessageManager.Instance.DisappearMessage().Forget();

        // 左クリック時はカーソル位置の床座標を保存する
        if (_clickKind == ClickKinds.Left)
        {
            _targetFloorPos = new Vector2Int(int.MinValue, int.MinValue);
            Ray ray = Camera.main.ScreenPointToRay(_clickMousePos);
            if (Physics.Raycast(ray, out RaycastHit rch, math.INFINITY, PlayerAttackPhase.LayerNumber))
            {
                Vector3 basePos = MapManager.Instance.GetBasePos();
                _targetFloorPos = new Vector2Int(
                    Mathf.RoundToInt(rch.point.x - basePos.x),
                    Mathf.RoundToInt(rch.point.z - basePos.z)
                );
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
