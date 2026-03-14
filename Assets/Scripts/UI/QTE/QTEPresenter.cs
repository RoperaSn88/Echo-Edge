using DG.Tweening;
using Cysharp.Threading.Tasks;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.InputSystem;
using System;

public class QTEPresenter : ObjectPooler
{
    [SerializeField]
    private Image _backGroundImage;

    [SerializeField]
    private Image _icon;

    [SerializeField]
    private Image _sliderBackGround;

    [SerializeField]
    private Slider _slider;

    [SerializeField]
    private Image _sliderImage;

    private MouseClick _mouseClick;

    private bool _clicked;

    private const float InitializeTime = 0.3f;

    private const float DisappearTime = 0.4f;

    [SerializeField]
    private float _result;

    public float Result => _result;

    public async override UniTask Appear()
    {
        _mouseClick = new MouseClick();
        _mouseClick.Mouse.MouseClick.started += Click;
        _mouseClick.Enable();

        // 初期化
        _slider.value = 0;
        _backGroundImage.color = SetAlphaColor(_backGroundImage.color, 0f);
        _icon.color = SetAlphaColor(_icon.color, 0f);
        _sliderBackGround.color = SetAlphaColor(_sliderBackGround.color, 0f);
        _backGroundImage.rectTransform.localScale = Vector3.zero;
        _icon.rectTransform.localScale = Vector3.zero;
        _sliderBackGround.rectTransform.localScale = Vector3.zero;

        // 出現
        await UniTask.WhenAll(
            _backGroundImage.DOFade(1f, InitializeTime).ToUniTask(),
            _backGroundImage.rectTransform.DOScale(Vector3.one, InitializeTime).SetEase(Ease.OutQuad).ToUniTask(),
            _icon.DOFade(1f,InitializeTime).ToUniTask(),
            _icon.rectTransform.DOScale(Vector3.one, InitializeTime).SetEase(Ease.OutQuad).ToUniTask(),
            _sliderBackGround.DOFade(1f, InitializeTime).ToUniTask(),
            _sliderBackGround.rectTransform.DOScale(Vector3.one, InitializeTime).SetEase(Ease.OutQuad).ToUniTask()
        );

        // 待機
        await UniTask.Delay(TimeSpan.FromSeconds(0.8f));

        var timer = Time.time;
        await UniTask.WhenAny(
            _slider.DOValue(1f, 0.8f).ToUniTask(),
            UniTask.WaitUntil(() => _clicked)
        );

        if (_clicked)
        {
            // 押した時間
            var endTime = Time.time;
            Debug.Log(endTime - timer);
            if(endTime - timer < 0.6f && endTime - timer > 0.3f)
            {
                // 成功
                await UniTask.WhenAll(
                    _backGroundImage.DOFade(0f, DisappearTime).ToUniTask(),
                    _backGroundImage.rectTransform.DOScale(Vector3.one * 1.2f, DisappearTime).SetEase(Ease.OutQuad).ToUniTask(),
                    _icon.DOFade(0f, DisappearTime).ToUniTask(),
                    _icon.rectTransform.DOScale(Vector3.one * 1.2f, DisappearTime).SetEase(Ease.OutQuad).ToUniTask(),
                    _sliderBackGround.DOFade(0f, DisappearTime).ToUniTask(),
                    _sliderBackGround.rectTransform.DOScale(Vector3.one * 1.2f, DisappearTime).SetEase(Ease.OutQuad).ToUniTask(),
                    _sliderImage.DOFade(0f, DisappearTime).ToUniTask()
                );
                Debug.Log("成功");
                _result = 1f;
            }
            else if (endTime - timer < 0.8f && endTime - timer >= 0.6f)
            {
                // 大成功
                await UniTask.WhenAll(
                    _backGroundImage.DOFade(0f, DisappearTime).ToUniTask(),
                    _backGroundImage.rectTransform.DOScale(Vector3.one * 1.5f, DisappearTime).SetEase(Ease.OutQuad).ToUniTask(),
                    _icon.DOFade(0f, DisappearTime).ToUniTask(),
                    _icon.rectTransform.DOScale(Vector3.one * 1.5f, DisappearTime).SetEase(Ease.OutQuad).ToUniTask(),
                    _sliderBackGround.DOFade(0f, DisappearTime).ToUniTask(),
                    _sliderBackGround.rectTransform.DOScale(Vector3.one * 1.5f, DisappearTime).SetEase(Ease.OutQuad).ToUniTask(),
                    _sliderImage.DOFade(0f, DisappearTime).ToUniTask()
                );
                Debug.Log("大成功");
                _result = 1.1f;
            }
        }

        // 失敗
        await UniTask.WhenAll(
            _backGroundImage.DOFade(0f,0.2f).ToUniTask(),
            _backGroundImage.rectTransform.DOScale(Vector3.zero, 0.2f).ToUniTask(),
            _icon.DOFade(0f,0.2f).ToUniTask(),
            _icon.rectTransform.DOScale(Vector3.zero, 0.2f).ToUniTask(),
            _sliderBackGround.DOFade(0f,0.2f).ToUniTask(),
            _sliderBackGround.rectTransform.DOScale(Vector3.zero, 0.2f).ToUniTask(),
            _sliderImage.DOFade(0f, 0.2f).ToUniTask()
        );
        Debug.Log("失敗");
        _result = 0.8f;
    }

    private void Click(InputAction.CallbackContext c)
    {
        _clicked = true;
    }

    private Color SetAlphaColor(Color baseColor, float a)
    {
        return new Color(baseColor.r, baseColor.g, baseColor.b, a);
    }
}
