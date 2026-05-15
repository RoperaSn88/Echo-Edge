using Cysharp.Threading.Tasks;

namespace UnityEngine.Selectable
{
    /// <summary>
    /// 選択時、次の選択を表示するテキストを管理するクラス
    /// </summary>
    public class NextText : TMPSelectObject
    {
        /// <summary>
        /// 次の選択肢を表示するグループ。選択されたときにこのグループに遷移する。
        /// </summary>
        [SerializeField]
        private RectTransform _selectableGroup;

        private SelectableGroup _group;

        private void Start()
        {
            _group = GetComponentInParent<SelectableGroup>();
        }

        public override async UniTask OnDecide()
        {
            AudioManager.Instance.PlaySe(SeAudioType.Click);
            _group.SetNextSelectableGroup(_selectableGroup);
            // 上に移動させる
            await SelectManager.Instance.PlaceAtTop(RectTransform);

            // 次の選択肢を表示する
            await _group.ShowNextGroup();
        }
    }
}