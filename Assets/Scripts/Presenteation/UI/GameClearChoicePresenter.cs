using Cysharp.Threading.Tasks;
using DG.Tweening;
using UnityEngine;

namespace EchoEdge.Presenter.UI
{
    /// <summary>
    /// ゲームクリア時に選べる行動。
    /// </summary>
    public enum GameClearChoice
    {
        /// <summary>
        /// ステージナンバーを+1し、次のステージへ進む。
        /// </summary>
        Continue,

        /// <summary>
        /// 準備シーンへ戻る。
        /// </summary>
        Rest,
    }

    /// <summary>
    /// ゲームクリア時に「続ける」「休む」の選択肢を表示し、選ばれた方を返すクラス。
    /// </summary>
    public class GameClearChoicePresenter : MonoBehaviour
    {
        private const float FadeDuration = 0.3f;

        private static GameClearChoicePresenter _instance;
        
        [SerializeField]
        private RectTransform _rectTransform;

        [SerializeField]
        private CanvasGroup _canvasGroup;

        /// <summary>
        /// 「続ける」ボタン。最終ステージでは非表示にする。
        /// </summary>
        [SerializeField]
        private PhaseButton _continueButton;

        /// <summary>
        /// 「休む」ボタン。
        /// </summary>
        [SerializeField]
        private PhaseButton _restButton;

        public static GameClearChoicePresenter Instance => _instance;

        private const float InitialPositionX = -100f;
        private const float TargetPositionX = -270f;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetStatic()
        {
            _instance = null;
        }

        private void Awake()
        {
            if (_instance != null && _instance != this)
            {
                Destroy(gameObject);
                return;
            }

            _instance = this;
            gameObject.SetActive(false);
        }

        private void OnDestroy()
        {
            if (_instance == this) _instance = null;
        }

        /// <summary>
        /// 選択肢を表示し、どちらかが押されるまで待つ。
        /// Time.timeScale が 0 でも動くよう、Tween は unscaled で再生する。
        /// </summary>
        /// <param name="canContinue">次のステージが存在するか。false のとき「続ける」は表示しない。</param>
        public async UniTask<GameClearChoice> ShowAndWaitAsync(bool canContinue)
        {
            _continueButton.gameObject.SetActive(canContinue);
            _continueButton.Initialize();
            _restButton.Initialize();
            _continueButton.Interactable = true;
            _restButton.Interactable = true;

            _canvasGroup.DOKill();
            _canvasGroup.alpha = 0f;
            _rectTransform.anchoredPosition = new Vector2(InitialPositionX, _rectTransform.anchoredPosition.y); 
            gameObject.SetActive(true);
            transform.SetAsLastSibling();

            // フェード中に押されても取りこぼさないよう、フェード前にリスナーを登録する
            var tcs = new UniTaskCompletionSource<GameClearChoice>();
            _continueButton.OnClick = () => tcs.TrySetResult(GameClearChoice.Continue);
            _restButton.OnClick = () => tcs.TrySetResult(GameClearChoice.Rest);

            var task1 = _canvasGroup.DOFade(1f, FadeDuration).SetUpdate(true).ToUniTask();
            var task2 = _rectTransform.DOAnchorPosX(TargetPositionX, FadeDuration).SetUpdate(true).ToUniTask();
            
            await UniTask.WhenAll(task1, task2);

            var choice = await tcs.Task;

            _continueButton.OnClick = null;
            _restButton.OnClick = null;
            _continueButton.Interactable = false;
            _restButton.Interactable = false;

            return choice;
        }

        public async UniTask CloseAsync()
        {
            await _canvasGroup.DOFade(0f, FadeDuration).SetUpdate(true);
            gameObject.SetActive(false);
        }
    }
}
