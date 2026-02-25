using Cysharp.Threading.Tasks;
using Unity.Mathematics;
using UnityEngine;
using UnityEngine.InputSystem;
public class PlayerAttackPhase: IPhase
{
    /// <summary>
    /// アタックフェーズのインスタンス
    /// </summary>
    private static PlayerAttackPhase _instance;

    /// <summary>
    /// 他のスクリプトから干渉するプロパティ
    /// </summary>
    public static PlayerAttackPhase Instance => _instance ??= new PlayerAttackPhase();

    public const int LayerNumber = 1;

    public async UniTask<IPhase> WaitPhase()
    {
        // 今のポインターの先の位置を取得
        CameraManager.Instance.ActMoveCameraToDefault();
        Ray ray = Camera.main.ScreenPointToRay(CameraManager.Instance.GetMousePosition());
        try
        {
            Physics.Raycast(ray, out RaycastHit rch, math.INFINITY,LayerNumber);
            await PlayerController.Instance.Move(rch.point);
        }
        catch
        {
            Debug.Log("Rayが正しくhitしませんでしたわ");
        }
        // 

        return EnemyPhase.Instance;
    }
}