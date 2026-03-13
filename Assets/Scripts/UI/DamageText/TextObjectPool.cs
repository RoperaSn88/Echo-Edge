using System.Collections.Generic;
using UnityEngine;

public class TextObjectPool : MonoBehaviour
{
    [SerializeField]
    private uint _initSize;
    
    [SerializeField]
    private TextObject objectToPool;

    private Stack<TextObject> stack;

    private void Start()
    {
        SetupPool();
    }

    private void SetupPool()
    {
        stack = new Stack<TextObject>();
        TextObject instance = null;
        for (int i = 0; i < _initSize; i++)
        {
            instance = Instantiate(objectToPool);
            instance.Pool = this;
            instance.gameObject.SetActive(false);
            stack.Push(instance);
        }
    }

    public TextObject GetPooledObject()
    {
        // プールの大きさが十分でない場合は、新しい PooledObjects をインスタンス化する
        if (stack.Count == 0)
        {
            TextObject newInstance = Instantiate(objectToPool);
            newInstance.Pool = this;
            return newInstance;
        }
        // それ以外の場合は、リストから次のものをグラブする
        TextObject nextInstance = stack.Pop();
        nextInstance.gameObject.SetActive(true);
        return nextInstance;
    }

    public void ReturnToPool(TextObject pooledObject)
	{
        stack.Push(pooledObject);
        pooledObject.gameObject.SetActive(false);
	}
}
