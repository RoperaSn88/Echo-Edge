using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using ColorUtility = UnityEngine.ColorUtility;
using DG.Tweening;
using Cysharp.Threading.Tasks;
using DG.Tweening.Core;
using DG.Tweening.Plugins.Options;

namespace UI
{
    public class PlayerStatusPresenter : MonoBehaviour
    {
        public static PlayerStatusPresenter Instance;
        
        /// <summary>
        /// 色の遷移の時間
        /// </summary>
        private const float ChangeColorTime = 0.5f;
        
        /// <summary>
        /// スライダーの遷移の時間
        /// </summary>
        private const float ChangeValueTime = 0.5f;
        
        [SerializeField, Tooltip("プレイヤーHPのUI")] 
        private PlayerHP _playerHp;
        
        // [SerializeField, Tooltip("城のUI")]
        // private CastleHP _castleHp;
        
        [SerializeField, Tooltip("エナジーのUI")]
        private Energy _energy;

        /// <summary>
        /// ピンチ状態に表示するグラデーションパネル
        /// </summary>
        [SerializeField]
        private Image _attentionGradiate;
        
        /// <summary>
        /// 通常時の色
        /// </summary>
        private Color DefaultColor;
        
        /// <summary>
        /// ピンチ時の色
        /// </summary>
        private Color RedColor;
        
        /// <summary>
        /// プレイヤーのバトルステータス
        /// </summary>
        private BattleStatus _playerBattleStatus;

        /// <summary>
        /// プレイヤーのバトルステータスを取得するプロパティ
        /// </summary>
        public BattleStatus PlayerBattleStatus => _playerBattleStatus;

        /// <summary>
        /// 初期値設定。コンストラクタでできたら嬉しいなぁ...
        /// </summary>
        private void Start()
        {
            ColorUtility.TryParseHtmlString("#6593E6", out DefaultColor);
            ColorUtility.TryParseHtmlString("#E67A65", out RedColor);
            _playerBattleStatus = PlayerSwordParameterHolder.GetBattleStatus();
            Instance = this;
        }

        public void SetPlayerHP(int value, int maxValue)
        {
            _playerHp.SetPlayerHP(value, maxValue);
            
            // HPが2割未満ならば赤色にする
            if(value < maxValue * 0.2f)
            {
                _playerHp.SetColorCode(RedColor);
                // _castleHp.SetColorCode(RedColor);
                _energy.SetColorCode(RedColor);
                ShowAttentionGradiate(AttentionKinds.Red);
            }
            else
            {
                _playerHp.SetColorCode(DefaultColor);
                // _castleHp.SetColorCode(DefaultColor);
                _energy.SetColorCode(DefaultColor);
                HideAttentionGradiate();
            }
        }
        
        public void SetCastleHP(int value, int maxValue)
        {
            // _castleHp.SetCastleHP(value, maxValue);
            
            // HPが2割未満ならば赤色にする
            if(value < maxValue * 0.2f)
            {
                _playerHp.SetColorCode(RedColor);
                // _castleHp.SetColorCode(RedColor);
                _energy.SetColorCode(RedColor);
                ShowAttentionGradiate(AttentionKinds.Red);
            }
            else
            {
                _playerHp.SetColorCode(DefaultColor);
                // _castleHp.SetColorCode(DefaultColor);
                _energy.SetColorCode(DefaultColor);
                HideAttentionGradiate();
            }
        }
        
        public void SetEnergy(float gaugeValue, int energy)
        {
            _energy.SetEnergy(gaugeValue, energy);
        }
        
        public void ShowAttentionGradiate(AttentionKinds kind)
        {
            switch (kind)
            {
                case AttentionKinds.Blue:
                    _attentionGradiate.color = DefaultColor;
                    break;
                case AttentionKinds.Red:
                    _attentionGradiate.color = RedColor;
                    break;
            }
            _attentionGradiate.gameObject.SetActive(true);
            _attentionGradiate.color = new Color(_attentionGradiate.color.r, _attentionGradiate.color.g, _attentionGradiate.color.b, 0);
            _attentionGradiate.DOFade((10/255), ChangeColorTime).SetEase(Ease.OutQuad);
        }

        public void HideAttentionGradiate()
        {
            _attentionGradiate.DOFade(0, ChangeColorTime).SetEase(Ease.OutQuad).OnComplete(() =>
            {
                _attentionGradiate.gameObject.SetActive(false);
            });
        }
        
        [System.Serializable]
        class PlayerHP
        {
            /// <summary>
            /// プレイヤーのHPスライダー
            /// </summary>
            [SerializeField] private Image _playerHpImage;
            
            /// <summary>
            /// プレイヤーのHPフレーム
            /// </summary>
            [SerializeField] private Image _playerHpFrame;

            /// <summary>
            /// プレイヤーのHPテキスト
            /// </summary>
            [SerializeField] private TextMeshProUGUI _playerHpText;

            private Tween _hpFillTween;
            private Tween _hpImageColorTween;
            private Tween _hpFrameColorTween;
            
            /// <summary>
            /// HPスライダーをセットする
            /// </summary>
            /// <param name="value"></param>
            /// <param name="maxValue"></param>
            public void SetPlayerHP(int value, int maxValue)
            {
                if (maxValue == 0) throw new DivideByZeroException();
                Debug.Log("value: " + (float)(value) / maxValue);
                if (_hpFillTween != null && _hpFillTween.active)
                {
                    _hpFillTween.Kill();
                }
                _hpFillTween = _playerHpFrame.DOFillAmount((float)(value) / (float)maxValue, ChangeValueTime).SetEase(Ease.OutQuad);
                _playerHpText.text = value + " / " + maxValue;
            }

            public void SetColorCode(Color color)
            {
                if (_hpImageColorTween != null && _hpImageColorTween.active)
                {
                    _hpImageColorTween.Kill();
                }
                if (_hpFrameColorTween != null && _hpFrameColorTween.active)
                {
                    _hpFrameColorTween.Kill();
                }
                _hpImageColorTween = _playerHpImage.DOColor(color, ChangeColorTime).SetEase(Ease.OutQuad);
                _hpFrameColorTween = _playerHpFrame.DOColor(color, ChangeColorTime).SetEase(Ease.OutQuad);
            }
        }
        
        [System.Serializable]
        class CastleHP
        {
            /// <summary>
            /// 城のHPスライダー
            /// </summary>
            [SerializeField] private Image _castleHpImage;
            
            /// <summary>
            /// 城のHPフレーム
            /// </summary>
            [SerializeField] private Image _castleHpFrame;
            
            /// <summary>
            /// 城のHPテキスト
            /// </summary>
            [SerializeField] private TextMeshProUGUI _castleHpText;
            
            public void SetCastleHP(int value, int maxValue)
            {
                _castleHpFrame.DOFillAmount((float)value / maxValue, ChangeValueTime).SetEase(Ease.OutQuad);
                _castleHpText.text = value + " / " + maxValue;
            }
            
            public void SetColorCode(Color color)
            {
                _castleHpImage.DOKill();
                _castleHpFrame.DOKill();
                _castleHpImage.DOColor(color, ChangeColorTime).SetEase(Ease.OutQuad);
                _castleHpFrame.DOColor(color, ChangeColorTime).SetEase(Ease.OutQuad);
            }
        }
        
        ////////////////////////////////////////
        
        [System.Serializable]
        class Energy
        {
            /// <summary>
            /// エナジー用のスライダー
            /// </summary>
            [SerializeField] private Image _energyImage;
            
            /// <summary>
            /// エナジー用のフレーム
            /// </summary>
            [SerializeField]
            private Image _energyFrame;

            /// <summary>
            /// 持っているエナジーのテキスト
            /// </summary>
            [SerializeField] private TextMeshProUGUI _energyHaveText;

            /// <summary>
            /// 最大数エナジーのテキスト
            /// </summary>
            [SerializeField] private TextMeshProUGUI _energyMaxText;
            
            public const int MaxEnergy = 10;
            
            private const float ChangeValueTime = 0.5f;
            private TweenerCore<float, float, FloatOptions> t;
            
            public void SetEnergy(float gaugeValue, int energy)
            {
                if (t != null && t.active)
                {
                    t.Kill();
                }
                
                t = _energyImage.DOFillAmount(gaugeValue, ChangeValueTime).SetEase(Ease.OutCubic);
                _energyHaveText.text = energy.ToString();
                _energyMaxText.text = MaxEnergy.ToString();
            }
            
            public void SetColorCode(Color color)
            {
                _energyImage.DOKill();
                _energyFrame.DOKill();
                _energyImage.DOColor(color, ChangeColorTime).SetEase(Ease.OutQuad);
                _energyFrame.DOColor(color, ChangeColorTime).SetEase(Ease.OutQuad);
            }
        }
    }
}

public enum AttentionKinds
{
    Blue,
    Red
}
