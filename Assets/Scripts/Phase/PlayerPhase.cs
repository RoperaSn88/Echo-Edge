using System;
using Cysharp.Threading.Tasks;
using UnityEngine;
using UnityEngine.InputSystem;
using Actions;

public class PlayerPhase: IPhase
{
    /// <summary>
    /// クリックされたか検知するブール
    /// </summary>
    [SerializeField]
    bool _clickFlug;

    /// <summary>
    /// クリックの種類を保存する
    /// </summary>
    [SerializeField]
    private ClickKinds _clickKind;

    /// <summary>
    /// プレイヤーフェーズのインスタンス
    /// </summary>
    private static PlayerPhase _instance;

    /// <summary>
    /// 他のスクリプトから干渉するプロパティ
    /// </summary>
    public static PlayerPhase Instance => _instance ??= new PlayerPhase();


    public async UniTask<IPhase> WaitPhase()
    {
        // 初期条件
        _clickFlug = false;
        Debug.Log("Player Phase");
        PlayerActions playerActions = new PlayerActions();
        EnableController(playerActions);

        // 押させるのを待つ
        await UniTask.WaitUntil(()=>_clickFlug);
        
        // 右クリックか左クリックか識別する。
        _clickFlug = false;

        switch (_clickKind)
        {
            case ClickKinds.Left:
                CameraManager.Instance.ActMoveCameraToTopAngle();
                await UniTask.WaitUntil(()=>_clickFlug);
                ResetController(playerActions);
                return PlayerAttackPhase.Instance;
            case ClickKinds.Right:
                // 右クリック時
                ResetController(playerActions);
                return PlayerWeaponPhase.Instance;
        }

        throw new InvalidOperationException("クリックがうまくできない謎のエラーです");
    }

    public void EnableController(PlayerActions playerActions)
    {
        playerActions.PlayerPhase.Attack.started += OnPressAttack;
        playerActions.PlayerPhase.Attack.canceled += OnReleaseAttack;
        playerActions.PlayerPhase.Skill.started += OnPressSkill;
        playerActions.Enable();
    }

    public void ResetController(PlayerActions playerActions)
    {
        playerActions.Dispose();
    }

    public void OnPressAttack(InputAction.CallbackContext context)
    {
        _clickKind = ClickKinds.Left;
        _clickFlug = true;
    }

    public void OnPressSkill(InputAction.CallbackContext context)
    {
        _clickKind = ClickKinds.Right;
        _clickFlug = true;
    }

    public void OnReleaseAttack(InputAction.CallbackContext context)
    {
        _clickFlug = true;
    }
}