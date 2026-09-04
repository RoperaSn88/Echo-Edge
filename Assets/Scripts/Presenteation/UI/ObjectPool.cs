using System.Collections.Generic;
using UnityEngine;
using Cysharp.Threading.Tasks;

namespace EchoEdge.Presenter.UI
{
    public abstract class ObjectPool : MonoBehaviour
    {
        public uint _initSize;

        public ObjectPooler objectToPool;

        public Stack<ObjectPooler> stack;


        private void Start()
        {
            SetupPool();
        }

        private void OnDestroy()
        {
            ReleasePool();
        }

        public abstract void SetupPool();

        public abstract UniTask<ObjectPooler> GetPooledObject();

        public void ReturnToPool(ObjectPooler pooledObject)
    	{
            // プールが既に破棄されている場合は、返却されたオブジェクトも不要なため破棄する
            if (stack == null)
            {
                if (pooledObject != null)
                {
                    Destroy(pooledObject.gameObject);
                }
                return;
            }

            stack.Push(pooledObject);
            pooledObject.gameObject.SetActive(false);
    	}

        /// <summary>
        /// プールが破棄される際に、スタック内に残っているオブジェクトを解放する。
        /// プール（Canvas等の別の親を持つ場合を含む）が破棄されても、
        /// 生成済みのオブジェクトが残り続けて不正な参照エラーが起きるのを防ぐ。
        /// </summary>
        private void ReleasePool()
        {
            if (stack == null)
            {
                return;
            }

            while (stack.Count > 0)
            {
                ObjectPooler pooledObject = stack.Pop();
                if (pooledObject != null)
                {
                    Destroy(pooledObject.gameObject);
                }
            }

            stack = null;
        }
    }
}
