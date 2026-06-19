using UnityEngine;
using Cysharp.Threading.Tasks;
using DG.Tweening;
using TMPro;
using System;

public class QTETextPooler : ObjectPooler
{
    [SerializeField]
    private TextMeshProUGUI _qteText;

    [SerializeField]
    private RectTransform _rectTransform;

    public async UniTask Appear(QTEResults result)
    {
        _qteText.alpha = 0;
        _rectTransform.localScale = Vector3.one;

        switch (result)
        {
            case QTEResults.Failed:
                _qteText.text = "Failed...";
                break;
            case QTEResults.Good:
                _qteText.text = "Good!";
                break;
            case QTEResults.Perfect:
                _qteText.text = "Perfect!!";
                break;
        }   

        _rectTransform.DOScale(Vector3.one * 1.5f, 0.8f).SetEase(Ease.OutQuad).SetUpdate(true).ToUniTask().Forget();
        await _qteText.DOFade(1f, 0.2f).SetUpdate(true).ToUniTask();
        await UniTask.Delay(TimeSpan.FromSeconds(0.3f));
        await _qteText.DOFade(0f, 0.2f).SetUpdate(true).ToUniTask();
    }
    
    // 引数なしは使う想定ないんで、空に
    // これどうにかできねぇかなぁ
    public override async UniTask Appear()
    {
        await UniTask.Yield();
    }
}
