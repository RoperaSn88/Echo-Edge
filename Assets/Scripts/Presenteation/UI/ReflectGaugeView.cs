using System.Threading;
using Cysharp.Threading.Tasks;
using DG.Tweening;
using UnityEngine;
using UnityEngine.InputSystem;

using EchoEdge.Infra.Audio;
using EchoEdge.Presenter.Player;

namespace EchoEdge.Presenter.UI
{
    /// <summary>
    /// 追加反射の分度器QTE (仕様: 1-1-5. 反射)。
    /// 青いエリアを反射角を中心に表示し、エリアの反対側の端から針を回転させる。
    /// クリックで針を止め、エリア内なら成功。
    /// 角度はすべて度数法で、分度器ローカルの 0 度 = 右端、180 度 = 左端 (反時計回り)。
    /// </summary>
    public class ReflectGaugeView : MonoBehaviour
    {
        public static ReflectGaugeView Instance { get; private set; }

        [SerializeField, Tooltip("表示・非表示を切り替える CanvasGroup")]
        private CanvasGroup _canvasGroup;

        [SerializeField, Tooltip("分度器全体。反射する向きに合わせて回転させる")]
        private RectTransform _content;

        [SerializeField, Tooltip("追加の反射ができるエリア")]
        private RadialFillRange _area;

        [SerializeField, Tooltip("針の回転軸。回転 0 で針が真上 (90 度) を向くこと")]
        private RectTransform _needle;

        [SerializeField, Range(0f, 180f), Tooltip("最初のエリアの幅 (度)。反射角を中心に表示し、追加反射のたびに半分になる")]
        private float _initialAreaWidth = 45f;

        [SerializeField, Min(1f), Tooltip("針の回転速度 (度/秒)")]
        private float _needleSpeed = 240f;

        [SerializeField, Min(0), Tooltip("針が端から端まで動く回数の上限。超えたら失敗。1 で片道、0 なら止めるまで往復し続ける")]
        private int _maxSweepCount = 1;

        [SerializeField, Min(0), Tooltip("1回の一閃で行える追加反射の上限。0 なら上限なし")]
        private int _maxAdditionalReflectCount = 0;

        [SerializeField, Min(0f), Tooltip("表示・非表示のフェード時間 (秒)")]
        private float _fadeDuration = 0.15f;

        [SerializeField, Min(0f), Tooltip("針を止めてから結果を見せる時間 (秒)")]
        private float _resultDuration = 0.35f;

        public float InitialAreaWidth => _initialAreaWidth;

        public int MaxAdditionalReflectCount => _maxAdditionalReflectCount;

        private void Awake()
        {
            Instance = this;

            if (_canvasGroup == null) _canvasGroup = GetComponent<CanvasGroup>();
            if (_canvasGroup != null)
            {
                _canvasGroup.alpha = 0f;
                _canvasGroup.blocksRaycasts = false;
                _canvasGroup.interactable = false;
            }

            // 分度器ローカルの角度 = 右端 0 度から反時計回り
            if (_area != null)
            {
                _area.OriginAngle = 90f;
                _area.Clockwise = false;
            }
        }

        private void OnDestroy()
        {
            if (Instance == this) Instance = null;
        }

        /// <summary>
        /// 分度器QTEを行う。エリア内で針を止めたら true。
        /// </summary>
        public async UniTask<bool> PlayAsync(ReflectGaugeLayout layout, CancellationToken cancellationToken = default)
        {
            if (_content != null) _content.localEulerAngles = new Vector3(0f, 0f, layout.Rotation);
            if (_area != null) _area.SetRange(layout.AreaMin, layout.AreaMax);
            SetNeedleAngle(layout.NeedleStart);

            var clicked = false;
            var input = new MouseClick();
            input.Mouse.MouseClick.started += _ => clicked = true;

            try
            {
                AudioManager.Instance.PlaySe(BattleSeType.QTE);
                await Fade(1f, cancellationToken);

                // フェード中のクリックは無視する
                clicked = false;
                input.Enable();

                var sweepDuration = 180f / _needleSpeed;
                var elapsed = 0f;
                var needleAngle = layout.NeedleStart;

                while (!clicked)
                {
                    elapsed += Time.deltaTime;
                    var sweeps = elapsed / sweepDuration;
                    if (_maxSweepCount > 0 && sweeps >= _maxSweepCount)
                    {
                        needleAngle = _maxSweepCount % 2 == 0 ? layout.NeedleStart : layout.NeedleEnd;
                        SetNeedleAngle(needleAngle);
                        break;
                    }

                    needleAngle = Mathf.Lerp(layout.NeedleStart, layout.NeedleEnd, Mathf.PingPong(sweeps, 1f));
                    SetNeedleAngle(needleAngle);
                    await UniTask.Yield(PlayerLoopTiming.Update, cancellationToken);
                }

                input.Disable();

                var success = clicked && layout.Contains(needleAngle);
                AudioManager.Instance.PlaySe(success ? BattleSeType.QTE_Good : BattleSeType.QTE_Bad);

                await UniTask.Delay(System.TimeSpan.FromSeconds(_resultDuration), cancellationToken: cancellationToken);
                return success;
            }
            finally
            {
                input.Dispose();
                Fade(0f, CancellationToken.None).Forget();
            }
        }

        private void SetNeedleAngle(float angle)
        {
            // 針は回転 0 で真上 (90 度) を向く
            if (_needle != null) _needle.localEulerAngles = new Vector3(0f, 0f, angle - 90f);
        }

        private async UniTask Fade(float alpha, CancellationToken cancellationToken)
        {
            if (_canvasGroup == null) return;
            _canvasGroup.DOKill();
            await _canvasGroup.DOFade(alpha, _fadeDuration).ToUniTask(cancellationToken: cancellationToken);
        }
    }
}
