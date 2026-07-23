using Cysharp.Threading.Tasks;
using DG.Tweening;
using UnityEngine;

public class NextWaveView: MonoBehaviour
{
    public static NextWaveView Instance { get; private set; }
    
    [SerializeField]
    private CanvasGroup _canvasGroup;
    
    [SerializeField]
    private ParticleSystem _particleSystem;
    
    [SerializeField]
    private RectTransform _textRect;
    
    private const float TextMoveDistance = 960f;

    private const float ShowTime = 2f;
    
    void Start()
    {
        Instance = this;
        _canvasGroup.alpha = 0f;
        _canvasGroup.gameObject.SetActive(false);
        _particleSystem.Stop();
    }
    
    public async UniTask ShowNextWave() 
    {
        AudioManager.Instance.PlaySe(SeAudioType.NextWave);
        _particleSystem.Play();
        _canvasGroup.gameObject.SetActive(true);
        
        await UniTask.WhenAll(
            _canvasGroup.DOFade(1f, 0.5f).ToUniTask(),
            _textRect.DOLocalMoveX(TextMoveDistance, ShowTime).SetEase(Ease.OutQuad).ToUniTask()
        );
    }
    
    public async UniTask HideNextWave() 
    {
        await UniTask.WhenAll(
            _canvasGroup.DOFade(0f, 0.5f).ToUniTask(),
            _textRect.DOScale(0.5f, 0.5f).SetEase(Ease.InQuad).ToUniTask()
        );
        _canvasGroup.gameObject.SetActive(false);
        _particleSystem.Stop();
    }
}