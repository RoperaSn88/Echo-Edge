using UnityEngine;

namespace CommonUI.Tutorial.Models
{
    /// <summary>
    /// テキストを設定するためのモデル
    /// </summary>
    [System.Serializable]
    public class TextModel
    {
        [TextArea, SerializeField, Tooltip("表示させるテキスト")]
        private string _text;
        /// <summary>
        /// 表示させるテキスト
        /// </summary>
        public string Text => _text;

        [SerializeField, Tooltip("テキスト表示中に使うコーチマークのモデル")]
        private CoachMarkModel _coachMark;
        /// <summary>
        /// テキスト表示中に使うコーチマークのモデル
        /// </summary>
        public CoachMarkModel CoachMark => _coachMark;

        [SerializeField, Tooltip("指アイコンを表示するかどうか")]
        private bool _isFingerIconEnabled;
        /// <summary>
        /// 指アイコンを表示するかどうか
        /// </summary>
        public bool IsFingerIconEnabled => _isFingerIconEnabled;

        [SerializeField, Tooltip("指アイコンの向き(有効時のみ)")]
        private FingerKinds _fingerIconDirection;
        /// <summary>
        /// 指アイコンの向き
        /// </summary>
        public FingerKinds FingerIconDirection => _fingerIconDirection;

        [SerializeField, Tooltip("エフェクトを有効にするか")]
        private bool _isEffectEnabled;
        /// <summary>
        /// エフェクトを有効にするか
        /// </summary>
        public bool IsEffectEnabled => _isEffectEnabled;
    }
}