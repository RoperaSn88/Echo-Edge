using System;
using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using UnityEngine.UI;
using UnityEngine;
using DG.Tweening;
using System.Threading.Tasks;
using Unity.VisualScripting;

public class SelectManager : MonoBehaviour
{
    public static SelectManager Instance;
    
    private const float FadeTime = 2.0f;
    [SerializeField]
    private Image _panel;
    
    [SerializeField]
    private RectTransform _topRectTransform;
    
    private Stack<RectTransform> _placingStack = new Stack<RectTransform>();
    private Stack<Vector3> _placingStackOriginPos = new Stack<Vector3>();

    [SerializeField] private Transform _defaultPosition;
    [SerializeField] private SelectableGroup _selectableGroup;

    public Vector3 DefaultLocalPosition => _defaultPosition.localPosition;

    /// <summary>
    /// スタックの最上位にあるRectTransform
    /// </summary>
    public RectTransform TopItem => _placingStack.Count > 0 ? _placingStack.Peek() : null;

    /// <summary>
    /// 指定されたRectTransformがトップ配置スタックに含まれているか確認する
    /// </summary>
    public bool IsPlacedAtTop(RectTransform rect) => _placingStack.Contains(rect);

    /// <summary>
    /// 起動時
    /// </summary>
    async void Start()
    {
        Instance = this;

        if (AudioManager.Instance == null)
        {
            throw new InvalidOperationException("AudioManager.Instance が見つかりません。シーンに AudioManager を配置してください。");
        }
        
        await AudioManager.Instance.PlayBgm(BgmAudioType.Title, true);
        
        _panel.gameObject.SetActive(true);
        var c = _panel.color;
        c.a = 1f;
        _panel.color = c;

        await _panel.DOFade(0f,FadeTime);
        _panel.gameObject.SetActive(false);
        Selecting().Forget();
    }

    /// <summary>
    /// 選択の管理をする
    /// </summary>
    /// <returns></returns>
    async UniTask Selecting()
    {
        ISelectableManager manager = _selectableGroup;
        while (manager != null)
        {
            manager = await manager.Selecting();
        }
    }
    
    public async UniTask PlaceAtTop(RectTransform rectTransform)
    {
        //対象の元の位置を保存しておく
        _placingStackOriginPos.Push(rectTransform.localPosition);
        
        // すでにスタックしてるやつらは右にズラす
        if (_placingStack.Count != 0)
        {
            foreach (var rect in _placingStack)
            {
                rect.DOLocalMoveX(rect.localPosition.x + 600f, 0.5f).SetEase(Ease.InQuad);
            }
        }

        await UniTask.Delay(TimeSpan.FromSeconds(0.5f));
        
        // 新しいやつをスタックしてるやつの上に置く
        await rectTransform.DOLocalMoveY(_topRectTransform.localPosition.y, 0.5f).SetEase(Ease.OutQuad);
        _placingStack.Push(rectTransform);
    }
    
    public async UniTask RemoveFromTop()
    {
        if (_placingStack.Count == 0) return;
        
        // 最上位にいたやつを元の位置に戻す
        var targetRect = _placingStack.Pop();
        var originPos = _placingStackOriginPos.Pop();
        targetRect.DOLocalMove(originPos, 0.5f).SetEase(Ease.OutQuad).ToUniTask().Forget();
        
        // スタックしてるやつらを左にズラす
        foreach (var rect in _placingStack)
        {
            rect.DOLocalMoveX(rect.localPosition.x - 600f, 0.5f).SetEase(Ease.InQuad);
        }
        
        await UniTask.Delay(TimeSpan.FromSeconds(0.5f));
    }
}
