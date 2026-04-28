using Cysharp.Threading.Tasks;
using DG.Tweening;
using UnityEngine;

/// <summary>
/// Preparing シーン専用のカメラ移動を管理するクラス。
/// あらかじめ設定した座標へ曲線移動する。
/// </summary>
public class PreparingCameraController : MonoBehaviour
{
    /// <summary>
    /// インスタンス
    /// </summary>
    public static PreparingCameraController Instance;

    /// <summary>
    /// 移動先の座標
    /// </summary>
    [SerializeField]
    private Transform _targetPosition;

    /// <summary>
    /// 曲線移動の中継点（経由地点）
    /// </summary>
    [SerializeField]
    private Transform[] _waypoints;

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
    /// あらかじめ設定した座標へ中継点を経由した曲線移動を行う。
    /// </summary>
    public async UniTask MoveToTarget()
    {
        if (_targetPosition == null)
        {
            Debug.LogWarning($"{nameof(PreparingCameraController)}: 移動先の座標が設定されていません。");
            return;
        }

        // 中継点 + 目標点の配列を構築
        var points = new Vector3[_waypoints.Length + 1];
        for (int i = 0; i < _waypoints.Length; i++)
        {
            points[i] = _waypoints[i].position;
        }
        points[_waypoints.Length] = _targetPosition.position;

        await transform.DOPath(points, _duration, PathType.CatmullRom)
            .SetEase(Ease.InOutQuad)
            .ToUniTask();
    }
}
