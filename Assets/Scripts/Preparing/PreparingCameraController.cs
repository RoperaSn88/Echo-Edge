using Cysharp.Threading.Tasks;
using DG.Tweening;
using UnityEngine;

/// <summary>
/// Preparing シーン専用のカメラ移動を管理するクラス。
/// あらかじめ設定した座標へ曲線移動する。前方向・右方向の双方向に遷移できる。
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
    /// 右方向の移動先
    /// </summary>
    [SerializeField]
    private Transform _rightTarget;

    /// <summary>
    /// 右方向の曲線移動中継点
    /// </summary>
    [SerializeField]
    private Transform[] _rightWaypoints;

    /// <summary>
    /// 移動にかける時間
    /// </summary>
    [SerializeField]
    private float _duration = 1.0f;

    private void Awake()
    {
        Instance = this;
    }

    /// <summary>
    /// 前方向（左）へ中継点を経由した曲線移動を行う。
    /// </summary>
    public async UniTask MoveToTarget()
    {
        await MoveAlongPath(_forwardWaypoints, _forwardTarget, "前方向");
    }

    /// <summary>
    /// 右方向へ中継点を経由した曲線移動を行う。
    /// </summary>
    public async UniTask MoveRight()
    {
        await MoveAlongPath(_rightWaypoints, _rightTarget, "右方向");
    }

    /// <summary>
    /// 指定された中継点と目標座標へ曲線移動する共通処理。
    /// </summary>
    private async UniTask MoveAlongPath(Transform[] waypoints, Transform target, string directionName)
    {
        if (target == null)
        {
            Debug.LogWarning($"{nameof(PreparingCameraController)}: {directionName}の移動先座標が設定されていません。");
            return;
        }

        var waypointCount = waypoints?.Length ?? 0;
        var points = new Vector3[waypointCount + 1];
        for (int i = 0; i < waypointCount; i++)
        {
            points[i] = waypoints[i].position;
        }
        points[points.Length - 1] = target.position;

        await transform.DOPath(points, _duration, PathType.CatmullRom)
            .SetEase(Ease.InOutQuad)
            .ToUniTask();
    }
}
