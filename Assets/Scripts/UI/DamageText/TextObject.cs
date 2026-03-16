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
    
    /// <summary>
    /// 生成した時の処理
    /// </summary>
    public async override UniTask Appear()
    {
        gameObject.SetActive(false);
    }
    
    /// <summary>
    /// アニメーションを実行させる。
    /// </summary>
    public async UniTask Appearing(Transform targetTransform)
    {
        var worldPos = targetTransform.position + new Vector3(1, 0, 0);

        _rectTransform.position = worldPos;  // + new Vector3(100, 0, 0);
        
        gameObject.SetActive(true);
        
        var spawnPos = _rectTransform.anchoredPosition.x - 1f;

        _tmp.color = new Color(_tmp.color.r, _tmp.color.g, _tmp.color.b, 0);
        await UniTask.WhenAll(
            _tmp.DOFade(1f, AppearTime).SetUpdate(true).ToUniTask(),
            _rectTransform.DOAnchorPosX(spawnPos, AppearTime).SetEase(Ease.OutQuad).SetUpdate(true).ToUniTask()
        );
        
        await UniTask.WaitUntil(()=>UIPresenter.Instance.CanFadeText);

        await UniTask.WhenAll(
            _tmp.DOFade(0f, DisappearTime).SetUpdate(true).ToUniTask(),
            _rectTransform.DOAnchorPosX(spawnPos - 1f, DisappearTime).SetEase(Ease.InQuad).SetUpdate(true).ToUniTask()
        );

        Release();
    }
    
    public void SetText(string text)
    {
        _tmp.text = text;
    }
}
