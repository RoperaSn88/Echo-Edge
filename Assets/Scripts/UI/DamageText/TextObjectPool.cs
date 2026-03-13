using System.Collections.Generic;
using UnityEngine;
using AndanteTribe.Utils.Unity;
public class TextObjectPool : MonoBehaviour
{
    [SerializeField]
    private uint _initSize;
    
    [SerializeField]
    private TextObject objectToPool;

    [SerializeField]
    private Transform _canvasTransform;

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
            instance = Instantiate(objectToPool, _canvasTransform);
            instance.Pool = this;
            instance.gameObject.SetActive(false);
            stack.Push(instance);
        }
    }

    [Button("出現")]
    public TextObject GetPooledObject()
    {
        // プールの大きさが十分でない場合は、新しい PooledObjects をインスタンス化する
        if (stack.Count == 0)
        {
            TextObject newInstance = Instantiate(objectToPool, _canvasTransform);
            newInstance.Pool = this;
            newInstance.Appear();
            return newInstance;
        }
        // それ以外の場合は、リストから次のものをグラブする
        TextObject nextInstance = stack.Pop();
        nextInstance.gameObject.SetActive(true);
        nextInstance.Appear();
        return nextInstance;
    }

    public void ReturnToPool(TextObject pooledObject)
	{
        stack.Push(pooledObject);
        pooledObject.gameObject.SetActive(false);
	}
}
