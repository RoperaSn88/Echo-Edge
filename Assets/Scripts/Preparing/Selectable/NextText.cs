using Cysharp.Threading.Tasks;
using DG.Tweening;
using Unity.VisualScripting;

namespace UnityEngine.Selectable
{
    /// <summary>
    /// 選択時、次の選択を表示するテキストを管理するクラス
    /// </summary>
    public class NextText: TMPSelectObject
    {
        /// <summary>
        /// 次の選択肢を表示するグループ
        /// </summary>
        [SerializeField]
        private RectTransform _nextRectTransformGroup;

        private const float Duration = 0.5f;
        
        public override async UniTask OnDecide()
        {
            // 親のVerticalLayoutGroupを取得し、無効化
            var layoutGroup = GetComponentInParent<UnityEngine.UI.VerticalLayoutGroup>();
            if (layoutGroup != null)
            {
                layoutGroup.enabled = false;
            }
            
            // 他の選択肢を非表示にする
            var otherSelectables = transform.parent.GetComponentsInChildren<RectTransform>();
            bool isFirst = true;
            foreach (var selectable in otherSelectables)
            {
                if (isFirst)
                {
                    isFirst = false;
                    continue;
                }
                if (selectable != this.RectTransform)
                {
                    if (selectable == otherSelectables[otherSelectables.Length - 1])
                    {
                        await selectable.DOLocalMoveX(800f, Duration).SetEase(Ease.InQuad).ToUniTask();
                    }
                    else
                    {
                        selectable.DOLocalMoveX(800f, Duration).SetEase(Ease.InQuad).ToUniTask();
                        await UniTask.Delay(100);
                    }
                }
            }
            
            // 上に移動させる
            await SelectManager.Instance.PlaceAtTop(RectTransform);
            
            // 次の選択肢を表示する
            _nextRectTransformGroup.gameObject.SetActive(true);
            await _nextRectTransformGroup.DOMove(SelectManager.Instance.DefaultPosition, Duration).SetEase(Ease.OutQuad).ToUniTask();
        }
    }
}