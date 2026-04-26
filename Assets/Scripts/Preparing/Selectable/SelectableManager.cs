using Cysharp.Threading.Tasks;
using UnityEngine;

/// <summary>
/// 現在選択中の選択肢を管理するクラス。
/// 決定済みの選択肢を追跡し、再選択されないよう管理する。
/// </summary>
public class SelectableManager : MonoBehaviour, UnityEngine.Selectable.ISelectableManager
{
    /// <summary>決定済みの選択肢</summary>
    private ISelectable _decidedItem;

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
    public async UniTask<UnityEngine.Selectable.ISelectableManager> Selecting()
    {
        ISelectable selected;
        do
        {
            selected = await RayCasterManager.Instance.Selecting();
        } while (selected == null || IsDecided(selected));

        MarkAsDecided(selected);
        await selected.OnDecide();
        return this;
    }
}