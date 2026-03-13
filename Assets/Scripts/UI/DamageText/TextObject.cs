using UnityEngine;

public class TextObject : MonoBehaviour
{
    private TextObjectPool _pool;
    public TextObjectPool Pool
    {
        get => _pool;
        set => _pool = value;
    }

    public void Release()
    {
        Pool.ReturnToPool(this);
    }
}
