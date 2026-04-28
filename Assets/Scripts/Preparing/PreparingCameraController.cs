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

    [Header("前方向（左）")]
    /// <summary>
    /// 前方向の移動先
    /// </summary>
    [SerializeField]
    private Transform _forwardTarget;

    /// <summary>
    /// 前方向の曲線移動中継点
    /// </summary>
    [SerializeField]
    private Transform[] _forwardWaypoints;

    [Header("右方向")]
    /// <summary>
    /// 右方向の移動先 X 座標
    /// </summary>
    [SerializeField]
    private float _rightTargetX;

    /// <summary>
    /// 移動にかける時間
    /// </summary>
    [SerializeField]
    private float _duration = 1.0f;

    private Vector3 _startPosition;

    private void Awake()
    {
        Instance = this;
        _startPosition = transform.position;
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
        await transform.DOPath(points, _duration, PathType.CatmullRom)
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
            points[i] = _forwardWaypoints[waypointCount - 1 - i].position;
        }
        points[points.Length - 1] = _startPosition;

        await transform.DOPath(points, _duration, PathType.CatmullRom)
            .SetEase(Ease.InOutQuad)
            .ToUniTask();
    }

    /// <summary>
    /// 右方向へ X 軸直線移動を行う。
    /// </summary>
    public async UniTask MoveRight()
    {
        await transform.DOMoveX(_rightTargetX, _duration)
            .SetEase(Ease.InOutQuad)
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
            points[i] = _forwardWaypoints[i].position;
        }
        points[points.Length - 1] = _forwardTarget.position;
        return points;
    }
}
