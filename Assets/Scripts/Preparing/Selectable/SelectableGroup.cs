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
    
    private const float Duration = 0.5f;

    /// <summary>
    /// 選択肢を決定済みとしてマークする
    /// </summary>
    public void MarkAsDecided(ISelectable selectable)
    {
        _decidedItem = selectable;
    }

    /// <summary>
    /// 選択肢が決定済みかどうかを確認する
    /// </summary>
    public bool IsDecided(ISelectable selectable)
    {
        return _decidedItem == selectable;
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

        var selectables = transform.GetComponentsInChildren<RectTransform>();
        bool isFirst = true;
        foreach (var selectable in selectables)
        {
            if (isFirst)
            {
                isFirst = false;
                continue;
            }

            selectable.TryGetComponent<ISelectable>(out var item);
            if (item == _decidedItem) continue;

            if (selectable == selectables[selectables.Length - 1])
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
    /// 次の選択肢グループを表示して所定位置へ移動させる
    /// </summary>
    public async UniTask ShowNextGroup()
    {
        _nextRectTransformGroup.gameObject.SetActive(true);
        await _nextRectTransformGroup.DOMove(SelectManager.Instance.DefaultPosition, Duration).SetEase(Ease.OutQuad).ToUniTask();
    }

    /// <summary>
    /// 戻り先グループを表示して所定位置へ移動させる
    /// </summary>
    public async UniTask ShowBackGroup()
    {
        if (_backRectTransformGroup != null)
        {
            _backRectTransformGroup.gameObject.SetActive(true);

            var pos = _backRectTransformGroup.localPosition;
            _backRectTransformGroup.localPosition = new Vector3(-800f, pos.y, pos.z);

            await _backRectTransformGroup.DOMove(SelectManager.Instance.DefaultPosition, 0.8f)
                .SetEase(Ease.OutQuad)
                .ToUniTask();

            var backLayout = _backRectTransformGroup.GetComponent<VerticalLayoutGroup>();
            if (backLayout != null)
            {
                backLayout.enabled = true;
            }
        }
    }
    
    /// <summary>
    /// 移動した選択肢を戻し、選択状態をリセットする
    /// </summary>
    public async UniTask ResetGroup()
    {
        var selectables = transform.GetComponentsInChildren<RectTransform>();
        bool isFirst = true;

        await SelectManager.Instance.RemoveFromTop(); 
        foreach (var selectable in selectables)
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
            
            if (selectable != _rectTransform)
            {
                if (selectable == selectables[selectables.Length - 1])
                {
                    await selectable.DOLocalMoveX(800f, Duration).SetEase(Ease.InQuad).ToUniTask();
                }
                else
                {
                    selectable.DOLocalMoveX(800f, Duration).SetEase(Ease.InQuad).ToUniTask().Forget();
                }
            }
        }
        
        MarkAsDecided(null);
        _verticalLayoutGroup.enabled = true;
    }
}