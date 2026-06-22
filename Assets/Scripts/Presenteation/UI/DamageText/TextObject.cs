using UnityEngine;
using DG.Tweening;
using Cysharp.Threading.Tasks;
using TMPro;
using System;

public class TextObject : ObjectPooler
{
    [SerializeField]
    private RectTransform _baseRectTransform;
    [SerializeField]
    private RectTransform _textRectTransform;

    [SerializeField]
    private TextMeshProUGUI _tmp;
    
    private const float AppearTime = 0.4f;
    private const float DisappearTime = 0.6f;

    /// <summary>
    /// ワールドにいる敵の位置に追従するかどうか
    /// </summary>
    private bool chaseCanvas = false;
    
    /// <summary>
    /// 追尾する敵の位置
    /// </summary>
    private Vector3 _targetPosition;

    public void Release()
    {
        Pool.ReturnToPool(this);
    }

    private void Update()
    {
        if (chaseCanvas)
        {
            var worldPos = Camera.main.WorldToScreenPoint(_targetPosition);
            _baseRectTransform.position = worldPos;
        }
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
    public async UniTask Appearing(Vector3 targetTransform)
    {
        _targetPosition = targetTransform;
        
        var worldPos = Camera.main.WorldToScreenPoint(targetTransform);
        
        //位置の初期化
        _baseRectTransform.position = worldPos;
        _textRectTransform.anchoredPosition = new Vector2(100, 0);
        
        // 追わせるようにする
        chaseCanvas = true;
        
        gameObject.SetActive(true);
        
        var spawnPos = _textRectTransform.anchoredPosition.x - 100f;

        _tmp.color = new Color(_tmp.color.r, _tmp.color.g, _tmp.color.b, 0);
        await UniTask.WhenAll(
            _tmp.DOFade(1f, AppearTime).SetUpdate(true).ToUniTask(),
            _textRectTransform.DOAnchorPosX(spawnPos, AppearTime).SetEase(Ease.OutQuad).SetUpdate(true).ToUniTask()
        );
        
        await UniTask.WaitUntil(()=>UIPresenter.Instance.CanFadeText);

        await UniTask.WhenAll(
            _tmp.DOFade(0f, DisappearTime).SetUpdate(true).ToUniTask(),
            _textRectTransform.DOAnchorPosX(spawnPos - 100f, DisappearTime).SetEase(Ease.InQuad).SetUpdate(true).ToUniTask()
        );

        chaseCanvas = false;
        
        Release();
    }
    
    public void SetText(string text)
    {
        _tmp.text = text;
    }
}
