using System.Collections.Generic;
using Cysharp.Threading.Tasks;

using EchoEdge.Presenter.UI;

namespace EchoEdge.Presenter.Battle
{
    /// <summary>
    /// 『シーカー』が投げるフラスコ（<see cref="FlaskObject"/>）のオブジェクトプール
    /// </summary>
    public class FlaskPool : ObjectPool
    {
        public override void SetupPool()
        {
            stack = new Stack<ObjectPooler>();
            for (int i = 0; i < _initSize; i++)
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
            if (stack == null || stack.Count == 0)
            {
                ObjectPooler newInstance = Instantiate(objectToPool, transform);
                newInstance.Pool = this;
                return newInstance;
            }

            await UniTask.CompletedTask;
            return stack.Pop();
        }
    }
}
