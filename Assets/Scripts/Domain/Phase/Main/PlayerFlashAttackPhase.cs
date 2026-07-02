using Cysharp.Threading.Tasks;
using Unity.Mathematics;
using UnityEngine;

/// <summary>
/// めちゃくちゃ早い一閃(瞬間移動で壁と敵を斬りつける攻撃)フェーズ
/// </summary>
public class PlayerFlashAttackPhase: IPhase
{
    /// <summary>
    /// 一閃フェーズのインスタンス
    /// </summary>
    private static PlayerFlashAttackPhase _instance;

    /// <summary>
    /// 他のスクリプトから干渉するプロパティ
    /// </summary>
    public static PlayerFlashAttackPhase Instance => _instance ??= new PlayerFlashAttackPhase();

    public async UniTask<IPhase> WaitPhase()
    {
        // 今のポインターの先の位置を取得
        CameraManager.Instance.ActMoveCameraToDefault();
        Ray ray = Camera.main.ScreenPointToRay(CameraManager.Instance.GetMousePosition());
        Physics.Raycast(ray, out RaycastHit rch, math.INFINITY, PlayerAttackPhase.LayerNumber);

        // プレイヤーの攻撃開始時に追尾を開始する
        CameraManager.Instance.StartTracking(PlayerController.Instance.transform);

        await PlayerController.Instance.FlashMove(rch.point);

        // 追尾を終了する
        CameraManager.Instance.StopTracking();

        return EnemyPhase.Instance;
    }
}
