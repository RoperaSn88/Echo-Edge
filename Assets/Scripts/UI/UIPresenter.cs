using Unity.VisualScripting;
using UnityEngine;
using Cysharp.Threading.Tasks;
using UI;
using UI.Energy;
using UI.QTE;
using DG.Tweening;
using TMPro;

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
    [SerializeField] private RectTransform _phaseTextRectTransform;
    [SerializeField] private TextMeshProUGUI _phaseText;
    [SerializeField] private float _phaseTextMoveOffset = 150f;
    [SerializeField] private float _phaseTextMoveDuration = 0.2f;

    private Vector2 _phaseTextVisiblePosition;

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

        if (_phaseTextRectTransform != null)
        {
            _phaseTextVisiblePosition = _phaseTextRectTransform.anchoredPosition;
            _phaseTextRectTransform.anchoredPosition = HiddenPhaseTextPosition;
        }

        if (_phaseText != null)
        {
            _phaseText.gameObject.SetActive(false);
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

    private Vector2 HiddenPhaseTextPosition => _phaseTextVisiblePosition + new Vector2(0, _phaseTextMoveOffset);

    public async UniTask AppearPhaseText(string text)
    {
        if (_phaseText == null || _phaseTextRectTransform == null)
        {
            return;
        }

        _phaseTextRectTransform.DOKill();
        _phaseText.text = text;
        _phaseText.gameObject.SetActive(true);
        _phaseTextRectTransform.anchoredPosition = HiddenPhaseTextPosition;
        await _phaseTextRectTransform.DOAnchorPos(_phaseTextVisiblePosition, _phaseTextMoveDuration).SetEase(Ease.OutQuad);
    }

    public async UniTask DisappearPhaseText()
    {
        if (_phaseText == null || _phaseTextRectTransform == null)
        {
            return;
        }

        _phaseTextRectTransform.DOKill();
        await _phaseTextRectTransform.DOAnchorPos(HiddenPhaseTextPosition, _phaseTextMoveDuration).SetEase(Ease.InQuad);
        _phaseText.gameObject.SetActive(false);
    }
}
