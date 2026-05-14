using Cysharp.Threading.Tasks;

namespace UnityEngine.Selectable
{
    /// <summary>
    /// 選択時、カメラを前方へ移動させ強化選択肢グループを表示するテキストを管理するクラス。
    /// WeaponNextText と同じパターンで強化画面へ遷移する。
    /// </summary>
    public class EnhancementNextText : TMPSelectObject
    {
        /// <summary>
        /// 強化グループの RectTransform。選択されたときにこのグループを表示する。
        /// </summary>
        [SerializeField]
        private RectTransform _selectableGroup;

        private SelectableGroup _group;

        private void Start()
        {
            _group = GetComponentInParent<SelectableGroup>();
            if (_group == null)
            {
                Debug.LogError($"{nameof(EnhancementNextText)}: SelectableGroup が親オブジェクトに見つかりません。");
            }
        }

        public override async UniTask OnDecide()
        {
            _group.SetNextSelectableGroup(_selectableGroup);

            // 上に移動させる
            await SelectManager.Instance.PlaceAtTop(RectTransform);

            // Preparing シーン専用カメラを前方へ移動させる
            if (PreparingCameraController.Instance != null)
            {
                var t1 = PreparingCameraController.Instance.MoveForward();
                var t2 = PreparingCameraController.Instance.RotateCamera();
                await UniTask.WhenAll(t1, t2);
            }

            // 強化グループを表示する
            await _group.ShowNextGroup();
        }
    }
}
