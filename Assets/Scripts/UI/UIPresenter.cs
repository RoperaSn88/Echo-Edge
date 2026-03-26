using Unity.VisualScripting;
using UnityEngine;
using Cysharp.Threading.Tasks;

public class UIPresenter : MonoBehaviour
{
    public static UIPresenter Instance { get; private set; }
    
    [SerializeField]
    private QTEPool _qtePool;
    public QTEPool QtePool => _qtePool;

    [SerializeField]
    private DamageTextPool _enemyDamageTextPool;
    public DamageTextPool EnemyDamageTextPool => _enemyDamageTextPool;

    private bool _canFadeText;
    public bool CanFadeText => _canFadeText;

    
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

    public async UniTask<float> AppearQTE()
    {
        var QTEObject = (QTEPresenter)await Instance.QtePool.GetPooledObject();
        var result = QTEObject.Result;
        QTEObject.Release();
        return result;
    }

    public async UniTask AppearDamageText(string value, Transform targetTrans)
    {
        // Instance.EnemyDamageTextPool.RegisterTarget(targetTrans);
        var tmp = (TextObject) await Instance.EnemyDamageTextPool.GetPooledObject();
        tmp.SetText(value);
        await tmp.Appearing(targetTrans);
    }

    public void FadeTexts()
    {
        _canFadeText = true;
    }

    public void ResetFade()
    {
        _canFadeText = false;
    }
}
