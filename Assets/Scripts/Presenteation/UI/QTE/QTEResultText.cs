using UnityEngine;
using Cysharp.Threading.Tasks;
using DG.Tweening;
using TMPro;

namespace EchoEdge.Presenter.UI
{
    public class QTEResultText : MonoBehaviour
    {
        [SerializeField]
        private TextMeshProUGUI _qteText;

        [SerializeField]
        private RectTransform _rectTransform;

        private Sequence _sequence;

        public async UniTask Appear(QTEResults result)
        {
            // 前回の表示が残っていれば打ち切り、今回の表示に置き換える
            _sequence?.Kill();

            gameObject.SetActive(true);
            _qteText.alpha = 0;
            _rectTransform.localScale = Vector3.one;

            switch (result)
            {
                case QTEResults.Failed:
                    _qteText.text = "Failed...";
                    break;
                case QTEResults.Good:
                    _qteText.text = "Good!";
                    break;
                case QTEResults.Perfect:
                    _qteText.text = "Perfect!!";
                    break;
            }

            var sequence = DOTween.Sequence()
                .Append(_qteText.DOFade(1f, 0.2f))
                .AppendInterval(0.3f)
                .Append(_qteText.DOFade(0f, 0.2f))
                .Insert(0f, _rectTransform.DOScale(Vector3.one * 1.5f, 0.8f).SetEase(Ease.OutQuad))
                .SetUpdate(true);
            _sequence = sequence;

            await sequence.ToUniTask();

            // 次の表示に置き換えられていたら、そちらが表示中なので隠さない
            if (_sequence != sequence) return;

            _sequence = null;
            if (this != null)
            {
                gameObject.SetActive(false);
            }
        }
    }
}
