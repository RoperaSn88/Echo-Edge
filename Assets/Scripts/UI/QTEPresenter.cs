using DG.Tweening;
using Cysharp.Threading.Tasks;
using UnityEngine;
using UnityEngine.UI;

public class QTEPresenter : MonoBehaviour
{
    [SerializeField]
    private Image _backGroundImage;

    [SerializeField]
    private Image _icon;

    [SerializeField]
    private Image _sliderBackGround;

    [SerializeField]
    private Slider _slider;

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        Initialize();
    }

    public async UniTask Initialize()
    {
        // 初期化
        _slider.value = 0;


    }

    // Update is called once per frame
    void Update()
    {
        
    }
}
