using System.Collections.Generic;
using UnityEngine;
using Cysharp.Threading.Tasks;
using UI.QTE;

public class QTETextPool : ObjectPool
{
    [SerializeField]
    private Transform _canvasTransform;
    
    private QTEResults _result;
    public QTEResults Result => (QTEResults)_result;

    public void SetResult(QTEResults result)
    {
        _result = result;
    }

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
            QTETextPooler newInstance = (QTETextPooler)Instantiate(objectToPool, _canvasTransform);
            newInstance.Pool = this;
            newInstance.Appear(_result).Forget();
            return newInstance;
        }
        // それ以外の場合は、リストから次のものをグラブする
        QTETextPooler nextInstance = (QTETextPooler)stack.Pop();
        nextInstance.gameObject.SetActive(true);
        nextInstance.Appear(_result).Forget();
        return nextInstance;
    }
}
