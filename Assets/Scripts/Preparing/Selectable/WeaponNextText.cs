using Cysharp.Threading.Tasks;

namespace UnityEngine.Selectable
{
    /// <summary>
    /// 選択時、カメラを移動させ武器パラメータ調整の選択肢グループを表示するテキストを管理するクラス
    /// </summary>
    public class WeaponNextText : TMPSelectObject
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
                Debug.LogError($"{nameof(WeaponNextText)}: SelectableGroup が親オブジェクトに見つかりません。");
            }
        }

        public override async UniTask OnDecide()
        {
            _group.SetNextSelectableGroup(_selectableGroup);
            
            // 上に移動させる
            await SelectManager.Instance.PlaceAtTop(RectTransform);

            // Preparing シーン専用カメラを移動させる
            if (PreparingCameraController.Instance != null)
            {
                await PreparingCameraController.Instance.MoveForward();
            }

            // 次の選択肢を表示する
            await _group.ShowNextGroup();
        }
    }
}
