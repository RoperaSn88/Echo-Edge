using Cysharp.Threading.Tasks;

namespace UnityEngine.Selectable{
    /// <summary>
    /// 選択時、前の選択肢へ戻すテキストを管理するクラス
    /// </summary>
    public class BackText : TMPSelectObject
    {
        private SelectableGroup _group;
        
        /// <summary>
        /// 次の選択肢を表示するグループ。選択されたときにこのグループに遷移する。
        /// </summary>
        [SerializeField]
        private RectTransform _selectableGroup;


        private void Start()
        {
            _group = GetComponentInParent<SelectableGroup>();
        }

        public override async UniTask OnDecide()
        {
            AudioManager.Instance.PlaySe(SeAudioType.Click);
            _group.SetBackSelectableGroup(_selectableGroup);
            await _group.MoveSelectables();
            // 戻り先グループを表示して所定位置へ
            
            await _group.ShowBackGroup();
        }
    }
}