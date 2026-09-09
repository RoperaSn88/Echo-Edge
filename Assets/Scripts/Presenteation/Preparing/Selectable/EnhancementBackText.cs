using UnityEngine;
using Cysharp.Threading.Tasks;

using EchoEdge.App.Preparing;

namespace EchoEdge.Presenter.Preparing
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
            // このグループの決定済みアイテムとして自身を登録しておく。
            // マネージャー(ルートグループ)側の _decidedItem には入るが、
            // 実際に退避処理を行う _group 側には入らないため、ここで明示的にマークして
            // MoveSelectables 内でサイズ(拡大表示)を元に戻せるようにする。
            _group.MarkAsDecided(this);
            await _group.MoveSelectables();

            // Preparing シーン専用カメラを始点へ戻す
            if (PreparingCameraController.Instance != null)
            {
                var t1 = PreparingCameraController.Instance.MoveBack();
                var t2 = PreparingCameraController.Instance.ResetRotateCamera();
                await UniTask.WhenAll(t1, t2);
            }

            // メインメニューグループを表示する
            await _group.ShowBackGroup();
        }
    }
}
