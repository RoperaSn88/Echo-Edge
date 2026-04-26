using DG.Tweening;
using Cysharp.Threading.Tasks;

namespace UnityEngine.Selectable{
    /// <summary>
    /// 選択時、前の選択肢へ戻すテキストを管理するクラス
    /// </summary>
    public class BackText : TMPSelectObject {
        
        
        private const float Duration = 0.5f;

        public override async UniTask OnDecide()
        {
            // 親のVerticalLayoutGroupを取得し、無効化
            var layoutGroup = GetComponentInParent<UnityEngine.UI.VerticalLayoutGroup>();
            if (layoutGroup != null)
            {
                layoutGroup.enabled = false;
            }

            // 現在グループを右に退避
            var otherSelectables = transform.parent.GetComponentsInChildren<RectTransform>();
            foreach (var selectable in otherSelectables)
            {
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

            // 戻り先グループを表示して所定位置へ
            if (_backRectTransformGroup != null)
            {
                _backRectTransformGroup.gameObject.SetActive(true);

                var pos = _backRectTransformGroup.localPosition;
                _backRectTransformGroup.localPosition = new Vector3(-800f, pos.y, pos.z);

                await _backRectTransformGroup .DOMove(SelectManager.Instance.DefaultPosition,0.8f)
                    .SetEase(Ease.OutQuad)
                    .ToUniTask();

                // 戻り先グループのレイアウトを再開
                var backLayout = _backRectTransformGroup.GetComponent<UnityEngine.UI.VerticalLayoutGroup>();
                if (backLayout != null)
                {
                    backLayout.enabled = true;
                }
            }
        }
    }
}