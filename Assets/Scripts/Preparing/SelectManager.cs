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
    [SerializeField] private SelectableManager _selectableManager;

    public Vector3 DefaultPosition => _defaultPosition.position;

    /// <summary>
    /// 起動時
    /// </summary>
    async void Start()
    {
        Instance = this;
        
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
        UnityEngine.Selectable.ISelectableManager manager = _selectableManager;
        while (manager != null)
        {
            manager = await manager.Selecting();
        }
    }
    
    public async UniTask PlaceAtTop(RectTransform rectTransform)
    {
        //対象の元の位置を保存しておく
        _placingStackOriginPos.Push(rectTransform.position);
        
        // すでにスタックしてるやつらは右にズラす
        if (_placingStack.Count != 0)
        {
            foreach (var rect in _placingStack)
            {
                rect.DOMoveX(rect.position.x + 600f, 0.5f).SetEase(Ease.InQuad);
            }
        }

        await UniTask.Delay(TimeSpan.FromSeconds(0.5f));
        
        // 新しいやつをスタックしてるやつの上に置く
        await rectTransform.DOMoveY(_topRectTransform.position.y, 0.5f).SetEase(Ease.OutQuad);
        _placingStack.Push(rectTransform);
    }
    
    public async UniTask RemoveFromTop()
    {
        if (_placingStack.Count == 0) return;
        
        // 最上位にいたやつを元の位置に戻す
        _placingStack.Pop();
        var originPos = _placingStackOriginPos.Pop();
        await _topRectTransform.DOMove(originPos, 0.5f).SetEase(Ease.OutQuad);
        
        // スタックしてるやつらを左にズラす
        foreach (var rect in _placingStack)
        {
            rect.DOMoveX(rect.position.x - 600f, 0.5f).SetEase(Ease.InQuad);
        }
    }
}
