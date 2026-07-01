using System;
using Cysharp.Threading.Tasks;
using UnityEngine.InputSystem;
using Actions;

public class PlayerPhase: IPhase
{
    /// <summary>
    /// クリックされたか検知するブール
    /// </summary>
    private bool _clickFlug;

    /// <summary>
    /// クリックの種類を保存する
    /// </summary>
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
        await UIPresenter.Instance.TurnChangeView.ShowTurnChange(TurnChangeKinds.PlayerTurn);
        PlayerActions playerActions = new PlayerActions();
        EnableController(playerActions);

        await UniTask.WaitUntil(() => _clickFlug);
        _clickFlug = false;
        ResetController(playerActions);

        switch (_clickKind)
        {
            case ClickKinds.Left:
                // 左クリック時は一閃の準備フェーズへ移行する
                return PlayerAttackPreparationPhase.Instance;
            case ClickKinds.Right:
                // 右クリック時
                return PlayerWeaponPhase.Instance;
        }

        throw new InvalidOperationException("クリックがうまくできない謎のエラーです");
    }

    public void EnableController(PlayerActions playerActions)
    {
        playerActions.PlayerPhase.Attack.started += OnPressAttack;
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
}
