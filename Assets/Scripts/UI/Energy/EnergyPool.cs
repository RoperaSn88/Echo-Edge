using System.Collections.Generic;
using UnityEngine;
using Cysharp.Threading.Tasks;

namespace UI.Energy
{
    public class EnergyPool: ObjectPool
    {
        public override void SetupPool()
        {
            this.stack = new Stack<ObjectPooler>();
            ObjectPooler instance = null;
            for (int i = 0; i < this._initSize; i++)
            {
                instance = Instantiate(objectToPool, transform);
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
                ObjectPooler newInstance = Instantiate(objectToPool, transform);
                newInstance.Pool = this;
                return newInstance;
            }
            // それ以外の場合は、リストから次のものをグラブする
            ObjectPooler nextInstance = stack.Pop();
            nextInstance.gameObject.SetActive(true);
            return nextInstance;
        }
    }
}