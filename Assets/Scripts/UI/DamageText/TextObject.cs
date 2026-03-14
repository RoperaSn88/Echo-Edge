using UnityEngine;
using DG.Tweening;
using Cysharp.Threading.Tasks;
using TMPro;
using System;

public class TextObject : ObjectPooler
{
    [SerializeField]
    private RectTransform _rectTransform;

    [SerializeField]
    private TextMeshProUGUI _tmp;
    
    private const float AppearTime = 0.4f;
    private const float DisappearTime = 0.6f;

    public void Release()
    {
        Pool.ReturnToPool(this);
    }

    public async override UniTask Appear()
    {
        var spawnPos = _rectTransform.anchoredPosition.x;

        _tmp.color = new Color(_tmp.color.r, _tmp.color.g, _tmp.color.b, 0);
        await UniTask.WhenAll(
            _tmp.DOFade(1f, AppearTime).ToUniTask(),
            _rectTransform.DOAnchorPosX(spawnPos - 100, AppearTime).SetEase(Ease.OutQuad).ToUniTask()
        );
        await UniTask.Delay(TimeSpan.FromSeconds(0.5f));
        await UniTask.WhenAll(
            _tmp.DOFade(0f, DisappearTime).ToUniTask(),
            _rectTransform.DOAnchorPosX(spawnPos - 200, DisappearTime).SetEase(Ease.InQuad).ToUniTask()
        );

        Release();
    }
}
