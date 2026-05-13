using Cysharp.Threading.Tasks;

namespace UnityEngine.Selectable
{
    /// <summary>
    /// 選択時、カメラを元の位置に戻してメインメニューグループへ戻るテキストを管理するクラス。
    /// WeaponBackText と同じパターンで強化画面から戻る。
    /// </summary>
    public class EnhancementBackText : TMPSelectObject
    {
        /// <summary>
        /// 戻り先（メインメニュー）グループの RectTransform
        /// </summary>
        [SerializeField]
        private RectTransform _selectableGroup;

        private SelectableGroup _group;

        private void Start()
        {
            _group = GetComponentInParent<SelectableGroup>();
            if (_group == null)
            {
                Debug.LogError($"{nameof(EnhancementBackText)}: SelectableGroup が親オブジェクトに見つかりません。");
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

            // メインメニューグループを表示する
            await _group.ShowBackGroup();
        }
    }
}
