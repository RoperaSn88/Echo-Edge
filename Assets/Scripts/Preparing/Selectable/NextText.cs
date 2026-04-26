using Cysharp.Threading.Tasks;

namespace UnityEngine.Selectable
{
    /// <summary>
    /// 選択時、次の選択を表示するテキストを管理するクラス
    /// </summary>
    public class NextText : TMPSelectObject
    {
        private SelectableGroup _group;

        private void Start()
        {
            _group = GetComponentInParent<SelectableGroup>();
        }

        public override async UniTask OnDecide()
        {
            // 上に移動させる
            await SelectManager.Instance.PlaceAtTop(RectTransform);

            // 次の選択肢を表示する
            await _group.ShowNextGroup();
        }
    }
}