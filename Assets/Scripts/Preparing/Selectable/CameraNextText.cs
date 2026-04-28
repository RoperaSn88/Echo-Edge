using Cysharp.Threading.Tasks;

namespace UnityEngine.Selectable
{
    /// <summary>
    /// 選択時、カメラを移動させ次の選択肢グループを表示するテキストを管理するクラス
    /// </summary>
    public class CameraNextText : TMPSelectObject
    {
        private SelectableGroup _group;

        private void Start()
        {
            _group = GetComponentInParent<SelectableGroup>();
            if (_group == null)
            {
                Debug.LogError($"{nameof(CameraNextText)}: SelectableGroup が親オブジェクトに見つかりません。");
            }
        }

        public override async UniTask OnDecide()
        {
            // 上に移動させる
            await SelectManager.Instance.PlaceAtTop(RectTransform);

            // Preparing シーン専用カメラを移動させる
            if (PreparingCameraController.Instance != null)
            {
                await PreparingCameraController.Instance.MoveToTarget();
            }

            // 次の選択肢を表示する
            await _group.ShowNextGroup();
        }
    }
}
