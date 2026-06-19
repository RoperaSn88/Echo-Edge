using System.Collections.Generic;
using UnityEngine;
using Cysharp.Threading.Tasks;

public abstract class ObjectPool : MonoBehaviour
{
    public uint _initSize;
    
    public ObjectPooler objectToPool;

    public Stack<ObjectPooler> stack;


    private void Start()
    {
        SetupPool();
    }

    public abstract void SetupPool();
    
    public abstract UniTask<ObjectPooler> GetPooledObject();

    public void ReturnToPool(ObjectPooler pooledObject)
	{
        stack.Push(pooledObject);
        pooledObject.gameObject.SetActive(false);
	}
}
