using Cysharp.Threading.Tasks;
using System.Collections.Generic;

namespace UnityEngine.Selectable
{
    /// <summary>
    /// 現在選択中の選択肢を管理するクラス。
    /// 決定済みの選択肢を追跡し、再選択されないよう管理する。
    /// </summary>
    public class SelectableManager : MonoBehaviour, ISelectableManager
    {
        public static SelectableManager Instance;

        /// <summary>決定済みの選択肢</summary>
        private readonly HashSet<ISelectable> _decidedItems = new HashSet<ISelectable>();

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;
        }

        /// <summary>
        /// 選択肢を決定済みとしてマークする
        /// </summary>
        public void MarkAsDecided(ISelectable selectable)
        {
            _decidedItems.Add(selectable);
        }

        /// <summary>
        /// 選択肢が決定済みかどうかを確認する
        /// </summary>
        public bool IsDecided(ISelectable selectable)
        {
            return _decidedItems.Contains(selectable);
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
            await selected.OnDecide();
            return this;
        }
    }
}