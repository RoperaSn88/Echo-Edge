using System.Collections.Generic;
using UnityEngine;
using Cysharp.Threading.Tasks;

/// <summary>
/// 残像オブジェクトのオブジェクトプール
/// </summary>
public class AfterimagePool : ObjectPool
{
    public override void SetupPool()
    {
        this.stack = new Stack<ObjectPooler>();
        for (int i = 0; i < this._initSize; i++)
        {
            ObjectPooler instance = Instantiate(objectToPool, transform);
            instance.Pool = this;
            instance.gameObject.SetActive(false);
            stack.Push(instance);
        }
    }

    public override async UniTask<ObjectPooler> GetPooledObject()
    {
        // プールが空の場合は新しいインスタンスを生成する
        if (stack.Count == 0)
        {
            ObjectPooler newInstance = Instantiate(objectToPool, transform);
            newInstance.Pool = this;
            return newInstance;
        }
        // プールから取り出す（SetActiveはAfterimageAppear内で行う）
        ObjectPooler nextInstance = stack.Pop();
        return nextInstance;
    }
}
