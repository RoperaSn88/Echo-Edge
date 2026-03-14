using Unity.VisualScripting;
using UnityEngine;

public class UIPresenter : MonoBehaviour
{
    public static UIPresenter Instance { get; private set; }
    
    void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
        }
        else
        {
            Destroy(gameObject);
        }
    }
    
    [SerializeField]
    private ObjectPool _qtePool;
    public ObjectPool QtePool => _qtePool;

    [SerializeField]
    private ObjectPool _enemyDamageTextPool;
    public ObjectPool EnemyDamageTextPool => _enemyDamageTextPool;
}
