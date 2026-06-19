using System.Collections.Generic;
using UnityEngine;
using Cysharp.Threading.Tasks;

public class DamageTextPool : ObjectPool
{
    [SerializeField]
    private Transform _canvasTransform;

    public override void SetupPool()
    {
        this.stack = new Stack<ObjectPooler>();
        ObjectPooler instance = null;
        for (int i = 0; i < this._initSize; i++)
        {
            instance = Instantiate(objectToPool, _canvasTransform);
            instance.Pool = this;
            instance.gameObject.SetActive(false);
            stack.Push(instance);
        }
    }

    public override async UniTask<ObjectPooler> GetPooledObject()
    {
        // プールの大きさが十分でない場合は、新しい PooledObjects をインスタンス化する
        if (stack.Count == 0)
        {
            ObjectPooler newInstance = Instantiate(objectToPool, _canvasTransform);
            newInstance.Pool = this;
            newInstance.Appear();
            return newInstance;
        }
        // それ以外の場合は、リストから次のものをグラブする
        ObjectPooler nextInstance = stack.Pop();
        nextInstance.gameObject.SetActive(true);
        await nextInstance.Appear();
        return nextInstance;
    }
}