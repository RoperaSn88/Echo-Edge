using AndanteTribe.Utils.Unity;
using Unity.Cinemachine;
using UnityEngine;
using System.Threading;
using Cysharp.Threading.Tasks;
using DG.Tweening;
using System;
using UnityEngine.InputSystem.Composites;

public class CameraManager : MonoBehaviour
{
    /// <summary>
    /// インスタンス
    /// </summary>
    public static CameraManager Instance;

    /// <summary>
    /// ステージ全体を見渡すカメラの場所
    /// </summary>
    [SerializeField]
    private Transform _defaultCameraPos;

    /// <summary>
    /// 対象のオブジェクト
    /// </summary>
    [SerializeField]
    private Vector3 _target;
    public Vector3 Target => _target;

    /// <summary>
    /// カメラのCinemachine
    /// </summary>
    [SerializeField]
    private CinemachineCamera _cinemachineCamera;

    private CinemachineThirdPersonFollow _cinemachineThirdPersonFollow;

    /// <summary>
    /// カメラが移動中のフラグ
    /// </summary>
    [SerializeField]
    private bool _cameraMoving;

    /// <summary>
    /// キャンセルトーケンソースのフィールド
    /// </summary>
    private CancellationTokenSource _cts;

    /// <summary>
    /// カメラの初期値
    /// </summary>
    private Vector3 _baseCameraPos;

    private MousePosition _mousePosition;

    /// <summary>
    /// 通常状態のカメラの角度
    /// </summary>
    private const float DefaultCameraAngle = 10;

    /// <summary>
    /// 上のときのzのオフセット
    /// </summary>
    private const float TopCameraOffset = 0f;

    /// <summary>
    /// 通常時のzのオフセット
    /// </summary>
    private readonly Vector3 DefaultCameraOffset = new Vector3(0,1f,1f);
    
    private readonly Vector3 ZoomCameraOffset = new Vector3(0,0, 1f);

    private readonly Vector3 PlayerZoomOffset = new Vector3(0f,-0.5f, 1f);
    
    /// <summary>
    /// 真上から見たときのカメラの角度
    /// </summary>
    private const float TopCameraAngle = 90;

    /// <summary>
    /// 真上から見たときのスプライトの角度
    /// </summary>
    private const float TopSpriteAngle = 90;

    /// <summary>
    /// 通常時のカメラの距離
    /// </summary>
    private const float DefaultCameraDistance = 11;

    /// <summary>
    /// 上のカメラの距離
    /// </summary>
    private const float TopCameraDistance = 9;

    /// <summary>
    /// ズーム時のカメラの距離
    /// </summary>
    private const float ZoomCameraDistance = 5;

    private const float PlayerWeaponCameraDistance = 2f;

    /// <summary>
    /// 非同期処理の待機時間
    /// </summary>
    private const float TokenTime = 0.25f;
    
    private const float PlayerWeaponTokenTime = 0.4f;



    void Start()
    {
        Instance = this;
        Initialize();
        _mousePosition = new MousePosition();
        _mousePosition.Enable();
    }

    /// <summary>
    /// 現在のマウスの位置を取得する
    /// </summary>
    /// <returns>マウスの位置</returns>
    public Vector2 GetMousePosition()
    {
        return _mousePosition.Mouse.MousePos.ReadValue<Vector2>();
    }

    /// <summary>
    /// 初期化
    /// </summary>
    public void Initialize()
    {
        _baseCameraPos = _defaultCameraPos.position;
        _cinemachineThirdPersonFollow = _cinemachineCamera.GetComponent<CinemachineThirdPersonFollow>();
    }

    /// <summary>
    /// カメラを対象のゲームオブジェクトにズームさせ始める。
    /// </summary>
    public async UniTask ActSetCameraTarget(Vector3 target)
    {
        // カメラが動いている最中ならばキャンセル
        if (_cameraMoving)
        {
            Debug.Log("キャンセルだ");
            _cts.Cancel();
        }

        // CancekkationTokenSourceを初期化
        _cts = new CancellationTokenSource();
        _target = target;

        try
        {
            await SetCameraTarget(_cts.Token);
        }
        catch(OperationCanceledException)
        {
            Debug.Log("キャンセル");
        }
    }

    /// <summary>
    /// カメラを対象のゲームオブジェクトにズームする。
    /// </summary>
    /// <param name="ct"></param>
    /// <returns></returns>
    private async UniTask SetCameraTarget(CancellationToken ct)
    {
        _cameraMoving = true;
        
        var cameraTween = DOTween.To(()=>_cinemachineThirdPersonFollow.CameraDistance,
            d => _cinemachineThirdPersonFollow.CameraDistance = d, 
            ZoomCameraDistance, 
            TokenTime)
            .SetEase(Ease.OutQuad)
            .SetUpdate(true);
            
        var positionTween = DOTween.To(()=>_defaultCameraPos.position,
            pos => _defaultCameraPos.position = pos, 
            _target, 
            TokenTime)
            .SetEase(Ease.OutQuad)
            .SetUpdate(true);
        
        var offsetTween = DOTween.To(()=>_cinemachineThirdPersonFollow.ShoulderOffset,
            pos => _cinemachineThirdPersonFollow.ShoulderOffset = pos, 
            ZoomCameraOffset, 
            TokenTime)
            .SetEase(Ease.OutQuad)
            .SetUpdate(true);

        try
        {
            await UniTask.WhenAll(
                cameraTween.ToUniTask(cancellationToken: ct),
                offsetTween.ToUniTask(cancellationToken: ct),
                positionTween.ToUniTask(cancellationToken: ct)
            );
            _cameraMoving = false;
        }
        catch(Exception)
        {
            cameraTween.Complete();
            positionTween.Complete();
            offsetTween.Complete();
            _cameraMoving = false;
        }
    }

    /// <summary>
    /// カメラをもとの位置に戻し始める。
    /// </summary>
    [Button("リセット")]
    public async UniTask ActResetCameraTarget()
    {
        // カメラが動いている最中ならばキャンセル
        if (_cameraMoving)
        {
            Debug.Log("キャンセルだ");
            _cts.Cancel();
        }

        // CancekkationTokenSourceを初期化
        _cts = new CancellationTokenSource();

        try
        {
            await ResetCameraTarget(_cts.Token);
        }
        catch(OperationCanceledException)
        {
            Debug.Log("キャンセル");
        }
    }

    /// <summary>
    /// カメラをもとの位置に戻す。
    /// </summary>
    /// <param name="ct"></param>
    /// <returns></returns>
    private async UniTask ResetCameraTarget(CancellationToken ct)
    {
        _cameraMoving = true;
        
        var cameraTween = DOTween.To(()=>_cinemachineThirdPersonFollow.CameraDistance,
            d => _cinemachineThirdPersonFollow.CameraDistance = d, 
            DefaultCameraDistance, 
            TokenTime)
            .SetEase(Ease.OutQuad)
            .SetUpdate(true);
            
        var positionTween = DOTween.To(()=>_defaultCameraPos.position,
            pos => _defaultCameraPos.position = pos, 
            _baseCameraPos, 
            TokenTime)
            .SetEase(Ease.OutQuad)
            .SetUpdate(true);
        
        var offsetTween = DOTween.To(()=>_cinemachineThirdPersonFollow.ShoulderOffset,
                pos => _cinemachineThirdPersonFollow.ShoulderOffset = pos, 
                DefaultCameraOffset, 
                TokenTime)
            .SetEase(Ease.OutQuad)
            .SetUpdate(true);

        var rotationTween = DOTween.To(()=>_defaultCameraPos.rotation.eulerAngles,
            pos => _defaultCameraPos.rotation = Quaternion.Euler(pos), 
            new Vector3(DefaultCameraAngle,360,0), 
            TokenTime)
            .SetEase(Ease.OutQuad)
            .SetUpdate(true);

        try
        {
            await UniTask.WhenAll(
                cameraTween.ToUniTask(cancellationToken: ct),
                positionTween.ToUniTask(cancellationToken: ct),
                offsetTween.ToUniTask(cancellationToken: ct),
                rotationTween.ToUniTask(cancellationToken: ct)
            );
            _cameraMoving = false;
        }
        catch(Exception)
        {
            cameraTween.Complete();
            positionTween.Complete();
            offsetTween.Complete();
            rotationTween.Complete();
            _cameraMoving = false;
        }
    }

    /// <summary>
    ///  カメラを上に移動させ始める。
    /// </summary>
    public async void ActMoveCameraToTopAngle()
    {
        // カメラが動いている最中ならばキャンセル
        if (_cameraMoving)
        {
            Debug.Log("キャンセルだ");
            _cts.Cancel();
        }

        // CancekkationTokenSourceを初期化
        _cts = new CancellationTokenSource();

        try
        {
            await MoveCameraToTopAngle(_cts.Token);
        }
        catch(OperationCanceledException)
        {
            Debug.Log("キャンセル");
        }
    }

    /// <summary>
    /// カメラを上に移動させる。
    /// </summary>
    /// <param name="ct"></param>
    /// <returns></returns>
    private async UniTask MoveCameraToTopAngle(CancellationToken ct)
    {
        _cameraMoving = true;
        
        var cameraTween = DOTween.To(()=>_cinemachineThirdPersonFollow.CameraDistance,
            d => _cinemachineThirdPersonFollow.CameraDistance = d, 
            TopCameraDistance, 
            TokenTime)
            .SetEase(Ease.OutQuad);
            
        var positionTween = DOTween.To(()=>_defaultCameraPos.rotation.eulerAngles.x,
            pos => _defaultCameraPos.rotation = Quaternion.Euler(pos,0,0), 
            TopCameraAngle, 
            TokenTime)
            .SetEase(Ease.OutQuad);

        var offsetTween = DOTween.To(()=>_cinemachineThirdPersonFollow.ShoulderOffset.z,
            pos =>_cinemachineThirdPersonFollow.ShoulderOffset = new Vector3(0,0,pos), 
            TopCameraOffset, 
            TokenTime)
            .SetEase(Ease.OutQuad);

        try
        {
            await UniTask.WhenAll(
                cameraTween.ToUniTask(cancellationToken: ct),
                positionTween.ToUniTask(cancellationToken: ct),
                offsetTween.ToUniTask(cancellationToken: ct)
            );
            _cameraMoving = false;
        }
        catch(Exception)
        {
            cameraTween.Complete();
            positionTween.Complete();
            offsetTween.Complete();
            _cameraMoving = false;
        }
    }

    /// <summary>
    /// カメラを上から元の位置に戻し始める
    /// </summary>
    public async UniTask ActMoveCameraToDefault()
    {
        // カメラが動いている最中ならばキャンセル
        if (_cameraMoving)
        {
            Debug.Log("キャンセルだ");
            _cts.Cancel();
        }

        // CancekkationTokenSourceを初期化
        _cts = new CancellationTokenSource();

        try
        {
            await MoveCameraToDefault(_cts.Token);
        }
        catch(OperationCanceledException)
        {
            Debug.Log("キャンセル");
        }
    }

    private async UniTask MoveCameraToDefault(CancellationToken ct)
    {
        _cameraMoving = true;
        
        var cameraTween = DOTween.To(()=>_cinemachineThirdPersonFollow.CameraDistance,
            d => _cinemachineThirdPersonFollow.CameraDistance = d, 
            DefaultCameraDistance, 
            TokenTime)
            .SetEase(Ease.OutQuad);
            
        var positionTween = DOTween.To(()=>_defaultCameraPos.rotation.eulerAngles.x,
            pos => _defaultCameraPos.rotation = Quaternion.Euler(pos,0,0), 
            DefaultCameraAngle, 
            TokenTime)
            .SetEase(Ease.OutQuad);

        var offsetTween = DOTween.To(()=>_cinemachineThirdPersonFollow.ShoulderOffset.z,
            pos => _cinemachineThirdPersonFollow.ShoulderOffset = new Vector3(0,0,pos), 
            DefaultCameraOffset.z, 
            TokenTime)
            .SetEase(Ease.OutQuad);

        var rotationTween = DOTween.To(()=>_defaultCameraPos.rotation.eulerAngles,
            pos => _defaultCameraPos.rotation = Quaternion.Euler(pos), 
            new Vector3(DefaultCameraAngle,0,0), 
            TokenTime)
            .SetEase(Ease.OutQuad);
        
        try
        {
            await UniTask.WhenAll(
                cameraTween.ToUniTask(cancellationToken: ct),
                positionTween.ToUniTask(cancellationToken: ct),
                offsetTween.ToUniTask(cancellationToken: ct),
                rotationTween.ToUniTask(cancellationToken: ct)
            );
            _cameraMoving = false;
        }
        catch(Exception)
        {
            cameraTween.Complete();
            positionTween.Complete();
            offsetTween.Complete();
            _cameraMoving = false;
        }
    }

    /// <summary>
    /// カメラを対象のゲームオブジェクトにズームさせ始める。
    /// </summary>
    public async UniTask ActPlayerWeaponZoom(Vector3 target)
    {
        // カメラが動いている最中ならばキャンセル
        if (_cameraMoving)
        {
            Debug.Log("キャンセルだ");
            _cts.Cancel();
        }

        // CancekkationTokenSourceを初期化
        _cts = new CancellationTokenSource();
        _target = target;

        try
        {
            await PlayerWeaponZoom(_cts.Token);
        }
        catch(OperationCanceledException)
        {
            Debug.Log("キャンセル");
        }
    }

    /// <summary>
    /// カメラを対象のゲームオブジェクトにズームする。
    /// </summary>
    /// <param name="ct"></param>
    /// <returns></returns>
    private async UniTask PlayerWeaponZoom(CancellationToken ct)
    {
        _cameraMoving = true;
        
        var cameraTween = DOTween.To(()=>_cinemachineThirdPersonFollow.CameraDistance,
            d => _cinemachineThirdPersonFollow.CameraDistance = d, 
            PlayerWeaponCameraDistance, 
            PlayerWeaponTokenTime)
            .SetEase(Ease.OutCubic)
            .SetUpdate(true);
            
        var positionTween = DOTween.To(()=>_defaultCameraPos.position,
            pos => _defaultCameraPos.position = pos, 
            _target + new Vector3(0.5f,0,0), 
            PlayerWeaponTokenTime)
            .SetEase(Ease.OutQuint)
            .SetUpdate(true);
        
        var offsetTween = DOTween.To(()=>_cinemachineThirdPersonFollow.ShoulderOffset,
            pos => _cinemachineThirdPersonFollow.ShoulderOffset = pos, 
            PlayerZoomOffset, 
            PlayerWeaponTokenTime)
            .SetEase(Ease.OutCubic)
            .SetUpdate(true);
        
        var rotationTween = DOTween.To(()=>_defaultCameraPos.rotation.eulerAngles,
            pos => _defaultCameraPos.rotation = Quaternion.Euler(pos), 
            new Vector3(DefaultCameraAngle,-40,0), 
            PlayerWeaponTokenTime)
            .SetEase(Ease.OutQuad)
            .SetUpdate(true);

        try
        {
            await UniTask.WhenAll(
                cameraTween
                    .SetEase(Ease.OutCubic)
                    .ToUniTask(cancellationToken: ct),
                offsetTween
                    .SetEase(Ease.OutQuint)
                    .ToUniTask(cancellationToken: ct),
                positionTween
                    .SetEase(Ease.OutQuad)
                    .ToUniTask(cancellationToken: ct),
                rotationTween
                    .SetEase(Ease.OutQuad)
                    .ToUniTask(cancellationToken: ct)
            );
            _cameraMoving = false;
        }
        catch(Exception)
        {
            cameraTween.Complete();
            positionTween.Complete();
            offsetTween.Complete();
            rotationTween.Complete();
            _cameraMoving = false;
        }
    }
}