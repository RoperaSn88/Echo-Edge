using UnityEngine;
using UnityEngine.VFX;
using Cysharp.Threading.Tasks;

/// <summary>
/// VFXエフェクト用のオブジェクトプール管理オブジェクト。
/// </summary>
public class VFXObject : ObjectPooler
{
    [SerializeField]
    private VisualEffect _vfx;
    
    private const string KindPropertyName = "ParticleCount";

    private const float LifeTime = 1f;

    /// <summary>
    /// プールから取り出した際の初期化処理。
    /// </summary>
    public override async UniTask Appear()
    {
        gameObject.SetActive(false);
        await UniTask.CompletedTask;
    }

    /// <summary>
    /// VFXエフェクトを出現させ、1秒後にプールへ返却する。
    /// </summary>
    /// <param name="kind">エフェクトの種類（攻撃・撃破）</param>
    /// <param name="position">出現位置</param>
    public async UniTaskVoid VFXAppear(VFXKinds kind, Vector3 position)
    {
        transform.position = position;
        _vfx.SetInt(KindPropertyName, (int)kind);
        gameObject.SetActive(true);
        _vfx.Play();

        await UniTask.Delay(System.TimeSpan.FromSeconds(LifeTime), cancellationToken: destroyCancellationToken);

        Release();
    }
}
