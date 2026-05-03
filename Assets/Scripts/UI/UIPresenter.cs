using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.UI;
using Cysharp.Threading.Tasks;
using DG.Tweening;
using UI;
using UI.Energy;
using UI.QTE;

public class UIPresenter : MonoBehaviour
{
    public static UIPresenter Instance { get; private set; }
    
    [SerializeField]
    private QTEPool _qtePool;
    public QTEPool QtePool => _qtePool;

    [SerializeField]
    private DamageTextPool _enemyDamageTextPool;
    public DamageTextPool EnemyDamageTextPool => _enemyDamageTextPool;
    
    [SerializeField]
    private EnergyPool _energyPool;
    public EnergyPool EnergyPool => _energyPool;
    
    [SerializeField]
    private PlayerStatusPresenter _playerStatusPresenter;
    public PlayerStatusPresenter PlayerStatusPresenter => _playerStatusPresenter;

    [SerializeField] private RectTransform _destination;

    [SerializeField, Tooltip("シーン開始時のフェードに使用するパネル")]
    private Image _fadePanel;

    [SerializeField, Tooltip("フェード時間")]
    private float _fadeDuration = 1.0f;

    private bool _canFadeText;
    public bool CanFadeText => _canFadeText;
    
    void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            var c = _fadePanel.color;
            c.a = 1f;
            _fadePanel.color = c;
        }
        else
        {
            Destroy(gameObject);
        }
    }

    public async UniTask<float> AppearQTE(QTEKinds kind)
    {
        var QTEObject = (QTEPresenter)await Instance.QtePool.GetPooledObject();
        QTEObject.Kind = kind;
        await QTEObject.Appear();
        var result = QTEObject.Result;
        QTEObject.Release();
        return result;
    }

    public async UniTask AppearDamageText(string value, Vector3 targetTrans)
    {
        // Instance.EnemyDamageTextPool.RegisterTarget(targetTrans);
        var tmp = (TextObject) await Instance.EnemyDamageTextPool.GetPooledObject();
        tmp.SetText(value);
        await tmp.Appearing(targetTrans);
    }
    
    public async UniTask AppearEnergy(Vector3 targetTrans, int energyValue)
    {
        var energyObject = (EnergyPresenter) await Instance.EnergyPool.GetPooledObject();
        energyObject.SetPosition(_destination, targetTrans, energyValue);
        await energyObject.Appear();
    }

    public void FadeTexts()
    {
        _canFadeText = true;
    }

    public void ResetFade()
    {
        _canFadeText = false;
    }

    /// <summary>
    /// パネルをフェードインします（シーン開始時の暗転を解除する）。
    /// </summary>
    public async UniTask FadeInAsync()
    {
        if (_fadePanel == null) return;
        var c = _fadePanel.color;
        c.a = 1f;
        _fadePanel.color = c;
        _fadePanel.gameObject.SetActive(true);
        await _fadePanel.DOFade(0f, _fadeDuration).ToUniTask();
        _fadePanel.gameObject.SetActive(false);
    }

    /// <summary>
    /// パネルをフェードアウトします（暗転する）。
    /// </summary>
    public async UniTask FadeOutAsync()
    {
        if (_fadePanel == null) return;
        var c = _fadePanel.color;
        c.a = 0f;
        _fadePanel.color = c;
        _fadePanel.gameObject.SetActive(true);
        await _fadePanel.DOFade(1f, _fadeDuration).ToUniTask();
    }
}
