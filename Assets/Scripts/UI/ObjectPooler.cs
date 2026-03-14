using System;
using UnityEngine;
using Cysharp.Threading.Tasks;
using Object = System.Object;

public abstract class ObjectPooler: MonoBehaviour
{
    private ObjectPool _pool;
    
    public ObjectPool Pool
    {
        get => _pool;
        set => _pool = value;
    }
    public abstract UniTask Appear();
    
    public void Release()
    {
        _pool.ReturnToPool(this);
    }

    public void SetPool(ObjectPool pool)
    {
        _pool = pool;
    }
}