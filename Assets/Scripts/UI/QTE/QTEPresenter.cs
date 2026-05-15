using DG.Tweening;
using Cysharp.Threading.Tasks;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.InputSystem;
using System;
using UI.QTE;

public class QTEPresenter : ObjectPooler
{
    [SerializeField]
    private Image _backGroundImage;

    [SerializeField]
    private Image _icon;

    [SerializeField]
    private Image _sliderBackGround;

    private MouseClick _mouseClick;

    private bool _clicked;
    
    /// <summary>
    /// 待つ最大の時間
    /// </summary>
    private const float WaitTime = 0.75f;

    /// <summary>
    /// 出現してから待つ時間
    /// </summary>
    private const float WaitWaitTime = 0.3f;
    
    private const float SuccessTime = 0.45f;
    private const float GreatSuccessTime = 0.6f;

    private const float InitializeTime = 0.2f;

    private const float DisappearTime = 0.4f;

    [SerializeField]
    private float _result;

    public float Result => _result;
    
    public QTEKinds Kind;

    public async override UniTask Appear()
    {
        _mouseClick = new MouseClick();
        _mouseClick.Mouse.MouseClick.started += Click;
        _mouseClick.Enable();

        // 初期化
        _sliderBackGround.fillAmount= 0;
        _backGroundImage.color = SetAlphaColor(_backGroundImage.color, 0f);
        _icon.color = SetAlphaColor(_icon.color, 0f);
        _sliderBackGround.color = SetAlphaColor(_sliderBackGround.color, 0f);
        _backGroundImage.rectTransform.localScale = Vector3.zero;
        _icon.rectTransform.localScale = Vector3.zero;
        _sliderBackGround.rectTransform.localScale = Vector3.zero;
        _clicked = false;

        // 出現
        AudioManager.Instance.PlaySe(SeAudioType.QTE);
        
        await UniTask.WhenAll(
            _backGroundImage.DOFade(1f, InitializeTime).SetUpdate(true).ToUniTask(),
            _backGroundImage.rectTransform.DOScale(Vector3.one, InitializeTime).SetEase(Ease.OutQuad).SetUpdate(true).ToUniTask(),
            _icon.DOFade(1f,InitializeTime).SetUpdate(true).ToUniTask(),
            _icon.rectTransform.DOScale(Vector3.one, InitializeTime).SetEase(Ease.OutQuad).SetUpdate(true).ToUniTask(),
            _sliderBackGround.DOFade(1f, InitializeTime).SetUpdate(true).ToUniTask(),
            _sliderBackGround.rectTransform.DOScale(Vector3.one, InitializeTime).SetEase(Ease.OutQuad).SetUpdate(true).ToUniTask()
        );

        // 待機
        await UniTask.Delay(TimeSpan.FromSeconds(WaitWaitTime), ignoreTimeScale : true);

        var timer = Time.unscaledTime;
        await UniTask.WhenAny(
            _sliderBackGround.DOFillAmount(1f, WaitTime).SetUpdate(true).ToUniTask(),
            UniTask.WaitUntil(() => _clicked)
        );

        if (_clicked)
        {
            // 押した時間
            var endTime = Time.unscaledTime;
            
            if(endTime - timer < GreatSuccessTime && endTime - timer > SuccessTime)
            {
                AudioManager.Instance.PlaySe(SeAudioType.QTE_Good);
                // 成功
                await UniTask.WhenAll(
                    _backGroundImage.DOFade(0f, DisappearTime).SetUpdate(true).ToUniTask(),
                    _backGroundImage.rectTransform.DOScale(Vector3.one * 1.2f, DisappearTime).SetEase(Ease.OutQuad).SetUpdate(true).ToUniTask(),
                    _icon.DOFade(0f, DisappearTime).SetUpdate(true).ToUniTask(),
                    _icon.rectTransform.DOScale(Vector3.one * 1.2f, DisappearTime).SetEase(Ease.OutQuad).SetUpdate(true).ToUniTask(),
                    _sliderBackGround.DOFade(0f, DisappearTime).SetUpdate(true).ToUniTask(),
                    _sliderBackGround.rectTransform.DOScale(Vector3.one * 1.2f, DisappearTime).SetEase(Ease.OutQuad).SetUpdate(true).ToUniTask()
                );
                
                switch (Kind)
                {
                    case QTEKinds.Attack:
                        _result = 1.1f;
                        break;
                    case QTEKinds.Defend:
                        _result = 0.8f;
                        break;
                }
                _mouseClick.Dispose();
                return;
            }
            else if (endTime - timer < WaitTime && endTime - timer >= GreatSuccessTime)
            {
                AudioManager.Instance.PlaySe(SeAudioType.QTE_Perfect);
                // 大成功
                await UniTask.WhenAll(
                    _backGroundImage.DOFade(0f, DisappearTime).SetUpdate(true).ToUniTask(),
                    _backGroundImage.rectTransform.DOScale(Vector3.one * 1.5f, DisappearTime).SetEase(Ease.OutQuad).SetUpdate(true).ToUniTask(),
                    _icon.DOFade(0f, DisappearTime).SetUpdate(true).ToUniTask(),
                    _icon.rectTransform.DOScale(Vector3.one * 1.5f, DisappearTime).SetEase(Ease.OutQuad).SetUpdate(true).ToUniTask(),
                    _sliderBackGround.DOFade(0f, DisappearTime).SetUpdate(true).ToUniTask(),
                    _sliderBackGround.rectTransform.DOScale(Vector3.one * 1.5f, DisappearTime).SetEase(Ease.OutQuad).SetUpdate(true).ToUniTask()
                );
                Debug.Log("大成功");
                switch (Kind)
                {
                    case QTEKinds.Attack:
                        _result = 1.3f;
                        break;
                    case QTEKinds.Defend:
                        _result = 0f;
                        break;
                }
                _mouseClick.Dispose();
                return;
            }
        }

        // 失敗
        AudioManager.Instance.PlaySe(SeAudioType.QTE_Bad);
        await UniTask.WhenAll(
            _backGroundImage.DOFade(0f,0.2f).SetUpdate(true).ToUniTask(),
            _backGroundImage.rectTransform.DOScale(Vector3.zero, 0.2f).SetUpdate(true).ToUniTask(),
            _icon.DOFade(0f,0.2f).SetUpdate(true).ToUniTask(),
            _icon.rectTransform.DOScale(Vector3.zero, 0.2f).SetUpdate(true).ToUniTask(),
            _sliderBackGround.DOFade(0f,0.2f).SetUpdate(true).ToUniTask(),
            _sliderBackGround.rectTransform.DOScale(Vector3.zero, 0.2f).SetUpdate(true).ToUniTask()
        );
        Debug.Log("失敗");
        switch (Kind)
        {
            case QTEKinds.Attack:
                _result = 0.8f;
                break;
            case QTEKinds.Defend:
                _result = 1.1f;
                break;
        }
        _mouseClick.Dispose();
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
