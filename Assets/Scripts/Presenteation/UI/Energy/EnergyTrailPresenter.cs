using UnityEngine;
using DG.Tweening;
using Cysharp.Threading.Tasks;

using EchoEdge.App.Battle;

namespace EchoEdge.Presenter.UI
{
    /// <summary>
    /// 敵に攻撃が当たったとき、敵の位置から PlayerInfo のエナジー表示へ飛んでいくトレイル（UIParticle）。
    /// 演出の量は獲得量に関係なく一定で、エナジーの加算・表示の反映はトレイルの先端がエナジー表示に着いた時点で行う。
    /// 手順は Assets/Documents/EnergyTrail.md。
    /// </summary>
    public class EnergyTrailPresenter : ObjectPooler
    {
        [SerializeField, Tooltip("トレイルの先端。この RectTransform を敵の位置からエナジー表示まで動かす（UIParticle を付けた GameObject）")]
        private RectTransform _head;

        [SerializeField, Tooltip("先端から出し続けるトレイル用のパーティクル（Looping・Simulation Space: World）")]
        private ParticleSystem _trailParticle;

        [SerializeField, Tooltip("先端がエナジー表示に着いたときに1回だけ再生するパーティクル（任意）")]
        private ParticleSystem _arrivalParticle;

        [SerializeField, Tooltip("敵の位置からエナジー表示に着くまでの秒数（ヒットストップの影響を受けない）")]
        private float _flightTime = 0.7f;

        [SerializeField, Tooltip("軌道の膨らみ。発生位置と到着位置の距離に対する割合（左右はランダム）")]
        private float _curveRate = 0.35f;

        [SerializeField, Tooltip("移動のイージング")]
        private Ease _ease = Ease.InQuad;

        [SerializeField, Tooltip("到着時にエナジー表示を弾ませる大きさ（0 で無効）")]
        private float _arrivalPunchScale = 0.15f;

        [SerializeField, Tooltip("到着後、残ったパーティクルが消えるのを待つ最大秒数")]
        private float _maxFadeOutTime = 2f;

        private const float ArrivalPunchTime = 0.25f;

        private RectTransform _destinationRect;
        private Vector3 _emitWorldPosition;
        private int _value;

        /// <summary>
        /// このトレイルのエナジーをもう反映したか（二重加算・取りこぼし防止）
        /// </summary>
        private bool _isApplied;

        private void Awake()
        {
            // 攻撃ヒット時はヒットストップで Time.timeScale がほぼ 0 になるため、パーティクルは実時間で動かす
            foreach (var ps in GetComponentsInChildren<ParticleSystem>(true))
            {
                var main = ps.main;
                main.useUnscaledTime = true;
            }
        }

        /// <summary>
        /// 到着先・発生位置（ワールド座標）・加算するエナジー量を設定する
        /// </summary>
        public void SetPosition(RectTransform destination, Vector3 emitWorldPosition, int value)
        {
            _destinationRect = destination;
            _emitWorldPosition = emitWorldPosition;
            _value = value;
            _isApplied = false;
        }

        public override async UniTask Appear()
        {
            var token = this.GetCancellationTokenOnDestroy();
            try
            {
                var parent = _head.parent as RectTransform;
                if (_destinationRect == null || parent == null || !TryGetEmitLocalPosition(parent, out var start))
                {
                    // 敵がカメラの後ろにいるなど、トレイルを出せないときはその場で反映する
                    ApplyEnergy();
                    Release();
                    return;
                }

                gameObject.SetActive(true);
                if (_head.localScale == Vector3.zero)
                {
                    // Scale 0 だと UIParticle が何も描画しないため、1 に戻す
                    Debug.LogWarning($"[EnergyTrail] {_head.name} の Scale が 0 のため 1 にしました。プレハブの Scale を (1,1,1) にしてください。", _head);
                    _head.localScale = Vector3.one;
                }
                _head.localPosition = start;
                StopParticles(true);
                if (_trailParticle != null) _trailParticle.Play(true);

                // 発生位置と到着位置の中点から垂直方向へずらした点を制御点にした2次ベジェで飛ばす
                var end = GetDestinationLocalPosition(parent);
                var dir = end - start;
                var perpendicular = new Vector2(-dir.y, dir.x);
                var side = Random.value < 0.5f ? -1f : 1f;
                var control = (start + end) * 0.5f + perpendicular * (_curveRate * side * Random.Range(0.6f, 1f));

                await DOVirtual.Float(0f, 1f, _flightTime, t =>
                    {
                        // エナジー表示やカメラが動いても追いかけられるよう、到着位置は毎フレーム取り直す
                        _head.localPosition = Bezier(start, control, GetDestinationLocalPosition(parent), t);
                    })
                    .SetEase(_ease)
                    .SetUpdate(true)
                    .SetLink(gameObject)
                    .ToUniTask(cancellationToken: token);

                _head.localPosition = GetDestinationLocalPosition(parent);

                // 先端がエナジー表示に着いた時点で加算・表示を反映する
                ApplyEnergy();
                PlayArrivalEffect();

                if (_trailParticle != null) _trailParticle.Stop(true, ParticleSystemStopBehavior.StopEmitting);
                await WaitParticlesFinished(token);

                StopParticles(true);
                Release();
            }
            finally
            {
                // シーン遷移などで途中で破棄されても、獲得したエナジーは失わないようにする
                if (!_isApplied)
                {
                    ApplyEnergy();
                }
            }
        }

        private void ApplyEnergy()
        {
            if (_isApplied) return;
            _isApplied = true;

            var energyResult = EnergyManager.AddEnergy(_value);
            if (PlayerStatusPresenter.Instance != null)
            {
                PlayerStatusPresenter.Instance.SetEnergy(energyResult.gaugeValue, energyResult.energyCount);
            }
        }

        private void PlayArrivalEffect()
        {
            if (_arrivalParticle != null)
            {
                _arrivalParticle.Play(true);
            }

            if (_arrivalPunchScale > 0f && _destinationRect != null)
            {
                // 連続で着いたときにスケールがずれないよう、前の弾みは完了させてから弾ませる
                _destinationRect.DOKill(true);
                _destinationRect.DOPunchScale(Vector3.one * _arrivalPunchScale, ArrivalPunchTime, 1, 0f)
                    .SetUpdate(true)
                    .SetLink(_destinationRect.gameObject);
            }
        }

        private async UniTask WaitParticlesFinished(System.Threading.CancellationToken token)
        {
            var elapsed = 0f;
            while (elapsed < _maxFadeOutTime && IsAnyParticleAlive())
            {
                await UniTask.Yield(PlayerLoopTiming.Update, token);
                elapsed += Time.unscaledDeltaTime;
            }
        }

        private bool IsAnyParticleAlive()
        {
            return (_trailParticle != null && _trailParticle.IsAlive(true))
                   || (_arrivalParticle != null && _arrivalParticle.IsAlive(true));
        }

        private void StopParticles(bool clear)
        {
            var behavior = clear ? ParticleSystemStopBehavior.StopEmittingAndClear : ParticleSystemStopBehavior.StopEmitting;
            if (_trailParticle != null) _trailParticle.Stop(true, behavior);
            if (_arrivalParticle != null) _arrivalParticle.Stop(true, behavior);
        }

        /// <summary>
        /// 敵のワールド座標を、先端の親 RectTransform のローカル座標に変換する
        /// </summary>
        private bool TryGetEmitLocalPosition(RectTransform parent, out Vector2 local)
        {
            local = default;
            var worldCamera = Camera.main;
            if (worldCamera == null) return false;

            var screen = worldCamera.WorldToScreenPoint(_emitWorldPosition);
            if (screen.z < 0f) return false;

            return RectTransformUtility.ScreenPointToLocalPointInRectangle(parent, screen, GetCanvasCamera(), out local);
        }

        /// <summary>
        /// エナジー表示の位置を、先端の親 RectTransform のローカル座標に変換する（Canvas の Render Mode に依存しない）
        /// </summary>
        private Vector2 GetDestinationLocalPosition(RectTransform parent)
        {
            var canvasCamera = GetCanvasCamera();
            var screen = RectTransformUtility.WorldToScreenPoint(canvasCamera, _destinationRect.position);
            RectTransformUtility.ScreenPointToLocalPointInRectangle(parent, screen, canvasCamera, out var local);
            return local;
        }

        private Camera GetCanvasCamera()
        {
            var canvas = GetComponentInParent<Canvas>();
            if (canvas == null) return null;
            canvas = canvas.rootCanvas;
            return canvas.renderMode == RenderMode.ScreenSpaceOverlay ? null : canvas.worldCamera;
        }

        private static Vector2 Bezier(Vector2 start, Vector2 control, Vector2 end, float t)
        {
            var u = 1f - t;
            return u * u * start + 2f * u * t * control + t * t * end;
        }
    }
}
