using UnityEngine;
using Cysharp.Threading.Tasks;

public abstract class ObjectPooler: MonoBehaviour
{
    private ObjectPool _pool;
    
    public ObjectPool Pool
    {
        get => _pool;
        set => _pool = value;
    }

    /// <summary>
    /// 出現するときの処理
    /// </summary>
    /// <returns></returns>
    public abstract UniTask Appear();
    
    /// <summary>
    /// プールに戻すときの処理
    /// </summary>
    public void Release()
    {
        _pool.ReturnToPool(this);
    }

    public void SetPool(ObjectPool pool)
    {
        _pool = pool;
    }
}