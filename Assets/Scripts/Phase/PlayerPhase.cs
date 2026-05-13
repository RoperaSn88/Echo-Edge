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
    private const float AimLineHeightOffset = 0.1f;
    private const float AimLineWidth = 0.08f;
    private static readonly Color AimLineColor = new Color(1f, 0.35f, 0.2f, 0.9f);
    private static LineRenderer _attackGuideLine;

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
        HideAttackGuideLine();
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
                    HideAttackGuideLine();
                    ResetController(playerActions);
                    return PlayerAttackPhase.Instance;
                case ClickKinds.Right:
                    // 右クリック時
                    HideAttackGuideLine();
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
        HideAttackGuideLine();
        _clickFlug = true;
    }

    public void OnPressPause(InputAction.CallbackContext context)
    {
        HideAttackGuideLine();
        _pauseFlug = true;
    }

    private static void UpdateAttackGuideLine()
    {
        if (Camera.main == null || PlayerController.Instance == null)
        {
            HideAttackGuideLine();
            return;
        }

        Ray pointerRay = Camera.main.ScreenPointToRay(CameraManager.Instance.GetMousePosition());
        if (!Physics.Raycast(pointerRay, out RaycastHit pointerHit, Mathf.Infinity, PlayerAttackPhase.LayerNumber))
        {
            HideAttackGuideLine();
            return;
        }

        Transform playerTransform = PlayerController.Instance.PlayerTransform;
        Vector3 lineStart = playerTransform.position;
        lineStart.y += AimLineHeightOffset;

        Vector3 lineEnd = pointerHit.point;
        lineEnd.y = lineStart.y;

        Vector3 direction = lineEnd - lineStart;
        float targetDistance = direction.magnitude;
        if (targetDistance <= Mathf.Epsilon)
        {
            HideAttackGuideLine();
            return;
        }

        Ray attackRay = new Ray(lineStart, direction.normalized);
        RaycastHit[] rayHits = Physics.RaycastAll(attackRay, targetDistance);
        float wallDistance = targetDistance;
        bool hasWall = false;

        for (int i = 0; i < rayHits.Length; i++)
        {
            if (!rayHits[i].collider.CompareTag("Wall")) continue;
            if (rayHits[i].distance < wallDistance)
            {
                wallDistance = rayHits[i].distance;
                hasWall = true;
            }
        }

        if (hasWall)
        {
            lineEnd = lineStart + attackRay.direction * wallDistance;
        }

        EnsureAttackGuideLine();
        _attackGuideLine.enabled = true;
        _attackGuideLine.positionCount = 2;
        _attackGuideLine.SetPosition(0, lineStart);
        _attackGuideLine.SetPosition(1, lineEnd);
    }

    private static void EnsureAttackGuideLine()
    {
        if (_attackGuideLine != null) return;

        GameObject lineObject = new GameObject("PlayerAttackGuideLine");
        _attackGuideLine = lineObject.AddComponent<LineRenderer>();
        _attackGuideLine.positionCount = 2;
        _attackGuideLine.startWidth = AimLineWidth;
        _attackGuideLine.endWidth = AimLineWidth;
        _attackGuideLine.startColor = AimLineColor;
        _attackGuideLine.endColor = AimLineColor;
        _attackGuideLine.useWorldSpace = true;
        _attackGuideLine.enabled = false;
        _attackGuideLine.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
        _attackGuideLine.receiveShadows = false;

        Shader shader = Shader.Find("Sprites/Default");
        if (shader != null)
        {
            _attackGuideLine.material = new Material(shader);
        }
    }

    private static void HideAttackGuideLine()
    {
        if (_attackGuideLine == null) return;
        _attackGuideLine.enabled = false;
    }
}
