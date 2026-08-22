using System;
using System.Threading;
using Actions;
using Cysharp.Threading.Tasks;
using Domain.Battle.PlayerAttack;
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

        // 通常・一閃の切り替え状態はPlayerAttackPreparationViewModelを唯一のデータソースとする
        var screen = PlayerAttackPreparationScreen.Instance;
        using var cts = new CancellationTokenSource();
        await screen.InitializeAsync(cts.Token);
        await screen.OnShowAsync(cts.Token);

        PlayerActions playerActions = new PlayerActions();
        EnableController(playerActions);

        // 入力があるまで待機する
        while (!_clickFlug)
        {
            UpdateAttackGuideLine();
            await UniTask.Yield();
        }

        _attackGuideLine.Hide();
        _attackGuideLine.Destroy();
        ResetController(playerActions);

        await screen.OnHideAsync(cts.Token);

        switch (_clickKind)
        {
            case ClickKinds.Left:
                bool isFlashing = screen.ScreenModel.PlayerAttackPreparationViewModel.IsFlashing;
                return isFlashing ? PlayerFlashAttackPhase.Instance : PlayerAttackPhase.Instance;
            case ClickKinds.Right:
                await CameraManager.Instance.ActMoveCameraToDefault();
                return PlayerPhase.Instance;
        }

        throw new InvalidOperationException("クリックがうまくできない謎のエラーです");
    }

    private void EnableController(PlayerActions playerActions)
    {
        playerActions.PlayerPhase.Attack.started += OnPressAttack;
        playerActions.PlayerPhase.Skill.started += OnPressSkill;
        playerActions.PlayerPhase.Scroll.performed += OnScroll;
        playerActions.PlayerPhase.ToggleFlash.started += OnPressToggleFlash;
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
        // マウスホイールの回転で、攻撃の種類（反射・貫通・爆発）を切り替える。
        // 状態の保持はPlayerAttackPreparationViewModelに一本化し、表示への反映はViewControllerに任せる。
        PlayerAttackPreparationScreen.Instance.ScreenModel.ToggleAttackMode();
    }

    private void OnPressToggleFlash(InputAction.CallbackContext context)
    {
        // マウスホイールの押し込みで、通常の一閃と、めちゃくちゃ早い一閃を切り替える。
        PlayerAttackPreparationScreen.Instance.ScreenModel.ToggleFlashMode();
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
