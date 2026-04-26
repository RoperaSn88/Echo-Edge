using Cysharp.Threading.Tasks;

namespace UnityEngine.Selectable{
    /// <summary>
    /// 選択時、前の選択肢へ戻すテキストを管理するクラス
    /// </summary>
    public class BackText : TMPSelectObject {

        private SelectableGroup _group;

        private void Start()
        {
            _group = GetComponentInParent<SelectableGroup>();
        }

        public override async UniTask OnDecide()
        {
            // 戻り先グループを表示して所定位置へ
            await _group.ShowBackGroup();
        }
    }
}