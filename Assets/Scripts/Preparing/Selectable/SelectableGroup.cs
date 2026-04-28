using System;
using DG.Tweening;
using Cysharp.Threading.Tasks;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 現在選択中の選択肢を管理するクラス。
/// 決定済みの選択肢を追跡し、再選択されないよう管理する。
/// </summary>
public class SelectableGroup : MonoBehaviour, ISelectableManager
{
    /// <summary>
    /// 決定済みの選択肢
    /// </summary>
    private ISelectable _decidedItem;
    
    [SerializeField]
    private VerticalLayoutGroup _verticalLayoutGroup;
    
    /// <summary>
    /// 次の選択肢を表示するグループ
    /// </summary>
    [SerializeField]
    private RectTransform _nextRectTransformGroup;
    
    /// <summary>
    /// 戻り先の選択肢グループ
    /// </summary>
    [SerializeField]
    private RectTransform _backRectTransformGroup;

    private RectTransform _rectTransform;
    
    private RectTransform[] _children;
    
    private const float Duration = 0.5f;

    private void Start()
    {
        _rectTransform = GetComponent<RectTransform>();
        _children = transform.GetComponentsInChildren<RectTransform>();
    }

    /// <summary>
    /// 選択肢を決定済みとしてマークする
    /// </summary>
    public void MarkAsDecided(ISelectable selectable)
    {
        if (_decidedItem is TMPSelectObject prevTmp) prevTmp.SetDecided(false);
        _decidedItem = selectable;
        if (_decidedItem is TMPSelectObject newTmp) newTmp.SetDecided(true);
    }

    /// <summary>
    /// 選択肢が決定済みかどうかを確認する
    /// </summary>
    public bool IsDecided(ISelectable selectable)
    {
        return _decidedItem == selectable;
    }
    
    public void SetNextSelectableGroup(RectTransform group)
    {
        _nextRectTransformGroup = group;
    }
    
    public void SetBackSelectableGroup(RectTransform group)
    {
        _backRectTransformGroup = group;
    }

    /// <summary>
    /// 選び始める時 - 決定済みでない選択肢から一つ選ばせる
    /// </summary>
    public async UniTask<ISelectableManager> Selecting()
    {
        ISelectable selected;
        do
        {
            selected = await RayCasterManager.Instance.Selecting();
        } while (selected == null || IsDecided(selected));

        MarkAsDecided(selected);
        
        await MoveSelectablesExcept();
        
        await selected.OnDecide();
        return this;
    }

    /// <summary>
    /// VerticalLayoutGroupを無効にして、決定済みの選択肢以外を右に退避させる
    /// </summary>
    public async UniTask MoveSelectablesExcept()
    {
        _verticalLayoutGroup.enabled = false;

        bool isFirst = true;
        foreach (var selectable in _children)
        {
            if (isFirst)
            {
                isFirst = false;
                continue;
            }

            selectable.TryGetComponent<ISelectable>(out var item);
            if (item == _decidedItem) continue;
            if (SelectManager.Instance.IsPlacedAtTop(selectable)) continue;

            if (selectable == _children[_children.Length - 1])
            {
                await selectable.DOLocalMoveX(800f, Duration).SetEase(Ease.InQuad).ToUniTask();
            }
            else
            {
                selectable.DOLocalMoveX(800f, Duration).SetEase(Ease.InQuad).ToUniTask();
                await UniTask.Delay(100);
            }
        }
    }
    
    /// <summary>
    /// 選択時、グループ全体を右に退避させる
    /// </summary>
    public async UniTask MoveSelectables()
    {
        await _rectTransform.DOLocalMoveX(_rectTransform.localPosition.x + 800f, Duration).SetEase(Ease.InQuad).ToUniTask();
        
        MarkAsDecided(null);
    }

    /// <summary>
    /// 次の選択肢グループを表示して所定位置へ移動させる
    /// </summary>
    public async UniTask ShowNextGroup()
    {
        _nextRectTransformGroup.gameObject.SetActive(true);
        await _nextRectTransformGroup.DOLocalMove(SelectManager.Instance.DefaultLocalPosition, Duration).SetEase(Ease.OutQuad).ToUniTask();
    }

    /// <summary>
    /// 戻り先グループの選択肢を元の位置に戻し、トップにある文字を元の位置へ戻す
    /// </summary>
    public async UniTask ShowBackGroup()
    {
        if (_backRectTransformGroup == null) return;

        // 戻り先グループの、トップ以外の選択肢を元の位置に戻す
        var topItem = SelectManager.Instance.TopItem;
        var backChildren = _backRectTransformGroup.GetComponentsInChildren<RectTransform>();
        bool isFirst = true;
        foreach (var child in backChildren)
        {
            if (isFirst)
            {
                isFirst = false;
                continue;
            }
            if (child == topItem) continue;
            
            child.DOLocalMoveX(-125f, Duration).SetEase(Ease.OutQuad);
        }

        // トップにある文字を元の位置に戻す
        await SelectManager.Instance.RemoveFromTop();

        // 戻り先グループのレイアウトを再開
        var backLayout = _backRectTransformGroup.GetComponent<VerticalLayoutGroup>();
        if (backLayout != null)
        {
            backLayout.enabled = true;
        }
    }
    
    /// <summary>
    /// 移動した選択肢を戻し、選択状態をリセットする
    /// </summary>
    public async UniTask ResetGroup()
    {
        bool isFirst = true;

        await SelectManager.Instance.RemoveFromTop(); 
        foreach (var selectable in _children)
        {
            if (isFirst)
            {
                isFirst = false;
                continue;
            }

            selectable.TryGetComponent<ISelectable>(out var a);
            if (a == _decidedItem)
            {
                continue;
            }
            
            if (selectable == _children[_children.Length - 1])
            {
                await selectable.DOLocalMoveX(-125f, Duration).SetEase(Ease.InQuad).ToUniTask();
            }
            else
            {
                selectable.DOLocalMoveX(-125f, Duration).SetEase(Ease.InQuad).ToUniTask().Forget();
                await UniTask.Delay(100);
            }
        }
        
        await UniTask.Delay(TimeSpan.FromSeconds(Duration));
        
        MarkAsDecided(null);
        _verticalLayoutGroup.enabled = true;
    }
}