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
    /// ポーズ操作が行われたか検知するブール
    /// </summary>
    private bool _pauseFlug;

    private const int OptionSceneBuildIndex = 3;
    private readonly PlayerAttackGuideLine _attackGuideLine = new PlayerAttackGuideLine();

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
        _pauseFlug = false;
        _attackGuideLine.Hide();
        Debug.Log("Player Phase");
        PlayerActions playerActions = new PlayerActions();
        EnableController(playerActions);

        while (true)
        {
            // クリックまたはポーズ操作を待つ
            await UniTask.WaitUntil(() => _clickFlug || _pauseFlug);

            if (_pauseFlug)
            {
                _pauseFlug = false;
                _clickFlug = false;
                Time.timeScale = 0;
                OptionSceneController.CanRetire = true;
                await SceneLoader.AdditiveLoadAndWait(OptionSceneBuildIndex);
                OptionSceneController.CanRetire = false;
                Time.timeScale = 1;
                if (OptionSceneController.LastResult == OptionResult.Retire)
                {
                    _attackGuideLine.Destroy();
                    ResetController(playerActions);
                    SceneLoader.Load(GameScene.Preparing);
                    return PlayerPhase.Instance;
                }
                continue;
            }

            // 右クリックか左クリックか識別する。
            _clickFlug = false;

            switch (_clickKind)
            {
                case ClickKinds.Left:
                    CameraManager.Instance.ActMoveCameraToTopAngle();
                    while (!_clickFlug)
                    {
                        UpdateAttackGuideLine();
                        await UniTask.Yield();
                    }
                    _attackGuideLine.Hide();
                    _attackGuideLine.Destroy();
                    ResetController(playerActions);
                    return PlayerAttackPhase.Instance;
                case ClickKinds.Right:
                    // 右クリック時
                    _attackGuideLine.Hide();
                    _attackGuideLine.Destroy();
                    ResetController(playerActions);
                    return PlayerWeaponPhase.Instance;
            }

            throw new InvalidOperationException("クリックがうまくできない謎のエラーです");
        }
    }

    public void EnableController(PlayerActions playerActions)
    {
        playerActions.PlayerPhase.Attack.started += OnPressAttack;
        playerActions.PlayerPhase.Attack.canceled += OnReleaseAttack;
        playerActions.PlayerPhase.Skill.started += OnPressSkill;
        playerActions.PlayerPhase.Pause.started += OnPressPause;
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
        _attackGuideLine.Hide();
        _clickFlug = true;
    }

    public void OnPressPause(InputAction.CallbackContext context)
    {
        _attackGuideLine.Hide();
        _pauseFlug = true;
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
