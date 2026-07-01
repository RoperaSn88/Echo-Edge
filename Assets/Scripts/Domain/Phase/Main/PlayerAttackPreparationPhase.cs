using System;
using Actions;
using Cysharp.Threading.Tasks;
using UnityEngine;
using UnityEngine.InputSystem;

/// <summary>
/// 一閃(攻撃)の準備をするフェーズ
/// </summary>
public class PlayerAttackPreparationPhase : IPhase
{
    /// <summary>
    /// 一閃準備フェーズのインスタンス
    /// </summary>
    private static PlayerAttackPreparationPhase _instance;

    /// <summary>
    /// 他のスクリプトから干渉するプロパティ
    /// </summary>
    public static PlayerAttackPreparationPhase Instance => _instance ??= new PlayerAttackPreparationPhase();

    private readonly PlayerAttackGuideLine _attackGuideLine = new PlayerAttackGuideLine();

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
        // 初期条件
        _clickFlug = false;
        _attackGuideLine.SetMaterial(PlayerController.Instance.LineMaterial);
        CameraManager.Instance.ActMoveCameraToTopAngle();

        PlayerActions playerActions = new PlayerActions();
        EnableController(playerActions);

        while (!_clickFlug)
        {
            UpdateAttackGuideLine();
            await UniTask.Yield();
        }

        _attackGuideLine.Hide();
        _attackGuideLine.Destroy();
        ResetController(playerActions);

        switch (_clickKind)
        {
            case ClickKinds.Left:
                return PlayerAttackPhase.Instance;
            case ClickKinds.Right:
                return PlayerPhase.Instance;
        }

        throw new InvalidOperationException("クリックがうまくできない謎のエラーです");
    }

    private void EnableController(PlayerActions playerActions)
    {
        playerActions.PlayerPhase.Attack.started += OnPressAttack;
        playerActions.PlayerPhase.Skill.started += OnPressSkill;
        playerActions.PlayerPhase.Scroll.performed += OnScroll;
        playerActions.Enable();
    }

    private void ResetController(PlayerActions playerActions)
    {
        playerActions.Dispose();
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

    private void OnScroll(InputAction.CallbackContext context)
    {
        // マウスホイール操作。後ほど実装する。
    }

    private void UpdateAttackGuideLine()
    {
        if (Camera.main == null || PlayerController.Instance == null)
        {
            _attackGuideLine.Hide();
            return;
        }

        Ray pointerRay = Camera.main.ScreenPointToRay(CameraManager.Instance.GetMousePosition());
        if (!Physics.Raycast(pointerRay, out RaycastHit pointerHit, Mathf.Infinity, PlayerAttackPhase.LayerNumber))
        {
            _attackGuideLine.Hide();
            return;
        }

        _attackGuideLine.Update(PlayerController.Instance.PlayerTransform.position, pointerHit.point);
    }
}
