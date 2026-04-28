using Cysharp.Threading.Tasks;

namespace UnityEngine.Selectable
{
    /// <summary>
    /// 選択時、カメラを元の位置に戻して前の選択肢グループに戻るテキストを管理するクラス
    /// </summary>
    public class WeaponBackText : TMPSelectObject
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
            if (_group == null)
            {
                Debug.LogError($"{nameof(WeaponBackText)}: SelectableGroup が親オブジェクトに見つかりません。");
            }
        }

        public override async UniTask OnDecide()
        {
            _group.SetBackSelectableGroup(_selectableGroup);
            await _group.MoveSelectables();
            // Preparing シーン専用カメラを始点へ戻す
            if (PreparingCameraController.Instance != null)
            {
                await PreparingCameraController.Instance.MoveBack();
            }

            // 前の選択肢グループを表示する
            await _group.ShowBackGroup();
        }
    }
}
