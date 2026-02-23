using AndanteTribe.Utils.Unity;
using Unity.Cinemachine;
using UnityEngine;


public class CameraManager : MonoBehaviour
{
    /// <summary>
    /// ステージ全体を見渡すカメラの場所
    /// </summary>
    [SerializeField]
    private Transform _defaultCameraPos;
    
    [SerializeField]
    private Transform target;

    [SerializeField]
    private CinemachineCamera _cinemachineCamera;

    private CinemachineThirdPersonFollow _cinemachineThirdPersonFollow;

    /// <summary>
    /// カメラの初期値
    /// </summary>
    private Vector3 _baseCameraPos;

    /// <summary>
    /// 通常状態のカメラの角度
    /// </summary>
    public const float DefaultCameraAngle = 50;

    /// <summary>
    /// 通常状態のスプライトの角度
    /// </summary>
    public const float DefaultSpriteAngle = 30;

    /// <summary>
    /// 真上から見たときのカメラの角度
    /// </summary>
    public const float TopCameraAngle = 90;

    /// <summary>
    /// 真上から見たときのスプライトの角度
    /// </summary>
    public const float TopSpriteAngle = 90;

    /// <summary>
    /// 通常時のカメラの距離
    /// </summary>
    public const float DefaultCameraDistance = 11;

    /// <summary>
    /// 上のカメラの距離
    /// </summary>
    public const float TopCameraDistance = 9;

    /// <summary>
    /// ズーム時のカメラの距離
    /// </summary>
    public const float ZoomCameraDistance = 5;

    void Start()
    {
        Initialize();
    }

    public void Initialize()
    {
        _baseCameraPos = _defaultCameraPos.position;
        _cinemachineThirdPersonFollow = _cinemachineCamera.GetComponent<CinemachineThirdPersonFollow>();
        Debug.Log(_cinemachineThirdPersonFollow.gameObject.name);
    }

    // public void SetCameraTarget(Vector3 targetVec)
    // {
    //     _defaultCameraPos.position = targetVec;
    //     _cinemachineThirdPersonAim.AimDistance = 
    // }

    [Button("セット")]
    public void SetCameraTarget()
    {
        _cinemachineThirdPersonFollow.CameraDistance = ZoomCameraDistance;
        _defaultCameraPos.position = target.position;
    }

    /// <summary>
    /// カメラをもとの位置に戻す
    /// </summary>
    [Button("リセット")]
    public void ResetCameraTarget()
    {
        _cinemachineThirdPersonFollow.CameraDistance = DefaultCameraDistance;
        _defaultCameraPos.position = _baseCameraPos;
    }

    /// <summary>
    ///  カメラを上に移動させる
    /// </summary>
    public void MoveCameraToTopAngle()
    {
        _cinemachineThirdPersonFollow.CameraDistance = TopCameraDistance;
        _defaultCameraPos.rotation = Quaternion.Euler(TopCameraAngle,0,0);
    }

    /// <summary>
    /// カメラを上から元の位置に戻す
    /// </summary>
    public void MoveCameraToDefault()
    {
        _cinemachineThirdPersonFollow.CameraDistance = DefaultCameraDistance;
        _defaultCameraPos.rotation = Quaternion.Euler(DefaultCameraAngle,0,0);
    }
}