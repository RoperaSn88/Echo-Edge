using System;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.UI;
using Cysharp.Threading.Tasks;
using DG.Tweening;
using EchoEdge.App.Scene;

namespace EchoEdge.Presenter.UI
{
    public class UIPresenter : MonoBehaviour
    {
        public static UIPresenter Instance { get; private set; }

        [SerializeField]
        private QTEPresenter _qtePresenter;

        [SerializeField]
        private QTEResultText _qteResultText;

        [SerializeField]
        private DamageTextPool _enemyDamageTextPool;
        public DamageTextPool EnemyDamageTextPool => _enemyDamageTextPool;

        [SerializeField]
        private EnergyPool _energyPool;
        public EnergyPool EnergyPool => _energyPool;

        [SerializeField, Tooltip("エナジー獲得トレイル（EnergyTrailPresenter）のプール。Canvas の子に置く。未設定なら従来どおり敵の位置で光ってすぐ加算する")]
        private EnergyPool _energyTrailPool;

        [SerializeField]
        private PlayerStatusPresenter _playerStatusPresenter;
        public PlayerStatusPresenter PlayerStatusPresenter => _playerStatusPresenter;


        [SerializeField] private RectTransform _destination;

        [SerializeField, Tooltip("シーン開始時のフェードに使用するパネル")]
        private Image _fadePanel;

        [SerializeField, Tooltip("フェード時間")]
        private float _fadeDuration = 1.0f;

        [SerializeField] private CanvasGroup _canvasGroup;

        private bool _canFadeText;
        public bool CanFadeText => _canFadeText;

        private bool _isQTEPlaying;
        private UniTask<(float rate, QTEResults level)> _playingQTE;

        void Awake()
        {
            if (Instance == null)
            {
                Instance = this;
                var c = _fadePanel.color;
                c.a = 1f;
                _fadePanel.color = c;
            }
            else
            {
                Destroy(gameObject);
            }

            MainGamePauseController.Instance._CanvasGroup = _canvasGroup;
        }

        public async UniTask<float> AppearQTE(QTEKinds kind)
        {
            var result = await AppearQTEWithLevel(kind);
            return result.rate;
        }

        /// <summary>
        /// QTE を出し、倍率と判定（失敗/成功/大成功）を返す
        /// </summary>
        public UniTask<(float rate, QTEResults level)> AppearQTEWithLevel(QTEKinds kind)
        {
            // QTE の表示は1つだけなので、敵2体に同時に当たるなどで重なった要求は表示中の結果を共有する
            if (!_isQTEPlaying)
            {
                _isQTEPlaying = true;
                _playingQTE = PlayQTE(kind).Preserve();
            }
            return _playingQTE;
        }

        private async UniTask<(float rate, QTEResults level)> PlayQTE(QTEKinds kind)
        {
            try
            {
                _qtePresenter.Kind = kind;
                _qtePresenter.gameObject.SetActive(true);
                await _qtePresenter.Appear();
                return (_qtePresenter.Result, _qtePresenter.ResultLevel);
            }
            finally
            {
                _isQTEPlaying = false;
                if (_qtePresenter != null)
                {
                    _qtePresenter.gameObject.SetActive(false);
                }
            }
        }

        public async UniTask AppearQTEResult(QTEResults result)
        {
            await _qteResultText.Appear(result);
        }

        public async UniTask AppearDamageText(string value, Vector3 targetTrans)
        {
            // Instance.EnemyDamageTextPool.RegisterTarget(targetTrans);
            var tmp = (TextObject) await Instance.EnemyDamageTextPool.GetPooledObject();
            tmp.SetText(value);
            await tmp.Appearing(targetTrans);
        }

        public async UniTask AppearEnergy(Vector3 targetTrans, int energyValue)
        {
            if (_energyTrailPool != null)
            {
                // 敵の位置から PlayerInfo のエナジー表示へトレイルを飛ばし、先端が着いた時点で加算する
                var trail = (EnergyTrailPresenter) await _energyTrailPool.GetPooledObject();
                trail.SetPosition(_destination, targetTrans, energyValue);
                await trail.Appear();
                return;
            }

            var energyObject = (EnergyPresenter) await Instance.EnergyPool.GetPooledObject();
            energyObject.SetPosition(_destination, targetTrans, energyValue);
            await energyObject.Appear();
        }

        public void FadeTexts()
        {
            _canFadeText = true;
        }

        public void ResetFade()
        {
            _canFadeText = false;
        }

        /// <summary>
        /// パネルをフェードインします（シーン開始時の暗転を解除する）。
        /// </summary>
        public async UniTask FadeInAsync(float duration = -1f)
        {
            if (_fadePanel == null) return;
            if (duration < 0f) duration = _fadeDuration;
            var c = _fadePanel.color;
            c.a = 1f;
            _fadePanel.color = c;
            _fadePanel.gameObject.SetActive(true);
            await _fadePanel.DOFade(0f, duration).SetUpdate(true).ToUniTask();
            _fadePanel.gameObject.SetActive(false);
        }

        /// <summary>
        /// パネルをフェードアウトします（暗転する）。
        /// </summary>
        public async UniTask FadeOutAsync(float duration = -1f)
        {
            if (_fadePanel == null) throw new InvalidOperationException("_fadePanel が設定されていません。インスペクターで割り当ててください。");
            if (duration < 0f) duration = _fadeDuration;
            var c = _fadePanel.color;
            c.a = 0f;
            _fadePanel.color = c;
            _fadePanel.gameObject.SetActive(true);
            await _fadePanel.DOFade(1f, duration).SetUpdate(true).ToUniTask();
        }
        
        private void OnDestroy()
        {
            if (Instance == this)
            {
                Instance = null;
            }
            
            MainGamePauseController.Instance._CanvasGroup = null;
        }
    }
}
