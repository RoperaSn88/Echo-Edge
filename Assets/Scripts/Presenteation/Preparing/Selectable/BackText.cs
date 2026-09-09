using UnityEngine;
using Cysharp.Threading.Tasks;

namespace EchoEdge.Presenter.Preparing
{
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
            _group.SetBackSelectableGroup(_selectableGroup);
            // このグループの決定済みアイテムとして自身を登録しておく。
            // マネージャー(ルートグループ)側の _decidedItem には入るが、
            // 実際に退避処理を行う _group 側には入らないため、ここで明示的にマークして
            // MoveSelectables 内でサイズ(拡大表示)を元に戻せるようにする。
            _group.MarkAsDecided(this);
            await _group.MoveSelectables();
            // 戻り先グループを表示して所定位置へ
            
            await _group.ShowBackGroup();
        }
    }
}
