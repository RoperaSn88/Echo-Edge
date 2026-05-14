using Cysharp.Threading.Tasks;
using DG.Tweening;
using UnityEngine;

/// <summary>
/// Preparing シーン専用のカメラ移動を管理するクラス。
/// 前方向へ中継点を経由した曲線移動、その逆方向への復帰、右方向への直線移動をサポートする。
/// </summary>
public class PreparingCameraController : MonoBehaviour
{
    /// <summary>
    /// インスタンス
    /// </summary>
    public static PreparingCameraController Instance;
    
    [SerializeField]
    private Transform _cameraTransform;

    [Header("前方向（左）")]
    /// <summary>
    /// 前方向の移動先
    /// </summary>
    [SerializeField]
    private Vector3 _forwardTarget;

    /// <summary>
    /// 前方向の曲線移動中継点
    /// </summary>
    [SerializeField]
    private Vector3[] _forwardWaypoints;

    [Header("右方向")]
    /// <summary>
    /// 右方向の移動先 X 座標
    /// </summary>
    [SerializeField]
    private float _rightTargetX;

    /// <summary>
    /// 移動にかける時間
    /// </summary>
    private const float _forwardDuration = 2.5f;
    
    private const float _rightDuration = 2.5f;

    private Vector3 _startPosition;
    
    private Vector3 _defaultAngle;
    
    private readonly Vector3 _rotateVector = new Vector3(0.919f, -123.249f, 0.802f);

    private void Awake()
    {
        Instance = this;
        Time.timeScale = 1.0f;
        _startPosition = _cameraTransform.position;
        _defaultAngle = _cameraTransform.eulerAngles;
    }

    /// <summary>
    /// 前方向（左）へ中継点を経由した曲線移動を行う。
    /// </summary>
    public async UniTask MoveForward()
    {
        if (_forwardTarget == null)
        {
            Debug.LogWarning($"{nameof(PreparingCameraController)}: 前方向の移動先座標が設定されていません。");
            return;
        }

        var points = BuildForwardPath();
        await _cameraTransform.DOPath(points, _forwardDuration, PathType.CatmullRom)
            .SetEase(Ease.InOutQuad)
            .ToUniTask();
    }

    /// <summary>
    /// 前方向の中継点を逆順にたどって始点へ戻る曲線移動を行う。
    /// </summary>
    public async UniTask MoveBack()
    {
        var waypointCount = _forwardWaypoints?.Length ?? 0;
        var points = new Vector3[waypointCount + 1];
        for (int i = 0; i < waypointCount; i++)
        {
            if (_forwardWaypoints[waypointCount - 1 - i] == null) continue;
            points[i] = _forwardWaypoints[waypointCount - 1 - i];
        }
        points[points.Length - 1] = _startPosition;

        await _cameraTransform.DOPath(points, _forwardDuration, PathType.CatmullRom)
            .SetEase(Ease.InOutQuad)
            .ToUniTask();
    }

    /// <summary>
    /// 右方向へ X 軸直線移動を行う。
    /// </summary>
    public async UniTask MoveRight()
    {
        await _cameraTransform.DOMoveX(_rightTargetX, _rightDuration)
            .SetEase(Ease.InQuad)
            .ToUniTask();
    }

    /// <summary>
    /// 前方向の中継点と目標座標を結合したパスを構築する。
    /// </summary>
    private Vector3[] BuildForwardPath()
    {
        var waypointCount = _forwardWaypoints?.Length ?? 0;
        var points = new Vector3[waypointCount + 1];
        for (int i = 0; i < waypointCount; i++)
        {
            if (_forwardWaypoints[i] == null) continue;
            points[i] = _forwardWaypoints[i];
        }
        points[points.Length - 1] = _forwardTarget;
        return points;
    }
    
    public async UniTask RotateCamera()
    {
        await _cameraTransform.DORotate(_rotateVector, _rightDuration, RotateMode.Fast);
    }
    
    public async UniTask ResetRotateCamera()
    {
        await _cameraTransform.DORotate(_defaultAngle, _rightDuration, RotateMode.Fast);
    }
}
