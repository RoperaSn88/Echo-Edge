using System.Collections.Generic;
using UnityEngine;
using AndanteTribe.Utils.Unity;
public class ObjectPool : MonoBehaviour
{
    [SerializeField]
    private uint _initSize;
    
    [SerializeField]
    private ObjectPooler objectToPool;

    [SerializeField]
    private Transform _canvasTransform;

    private Stack<ObjectPooler> stack;

    private void Start()
    {
        SetupPool();
    }

    private void SetupPool()
    {
        stack = new Stack<ObjectPooler>();
        ObjectPooler instance = null;
        for (int i = 0; i < _initSize; i++)
        {
            instance = Instantiate(objectToPool, _canvasTransform);
            instance.Pool = this;
            instance.gameObject.SetActive(false);
            stack.Push(instance);
        }
    }

    [Button("出現")]
    public ObjectPooler GetPooledObject()
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
        nextInstance.Appear();
        return nextInstance;
    }

    public void ReturnToPool(ObjectPooler pooledObject)
	{
        stack.Push(pooledObject);
        pooledObject.gameObject.SetActive(false);
	}
}
