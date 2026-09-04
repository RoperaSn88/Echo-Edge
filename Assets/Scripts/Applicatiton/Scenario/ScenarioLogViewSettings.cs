using System;
using TMPro;
using UnityEngine;

namespace EchoEdge.App.Scenario
{
    /// <summary>
    /// <see cref="ScenarioLogView"/> のログ表示に関する設定値。
    /// フォント・文字サイズ・色・余白など、見た目の調整項目をまとめて保持する。
    /// インスペクターでは <see cref="ScenarioLogView"/> の1フィールドとして入れ子表示される。
    /// </summary>
    [Serializable]
    public class ScenarioLogViewSettings
    {
        [SerializeField] private TMP_FontAsset _fontAsset;
        [SerializeField] private float _speakerFontSize = 22f;
        [SerializeField] private float _bodyFontSize = 24f;
        [SerializeField] private Color _speakerColor = new(0.65f, 0.85f, 1f, 1f);
        [SerializeField] private Color _bodyColor = Color.white;

        [Tooltip("ログ1件ごとの縦間隔（px）。")]
        [SerializeField] private float _entrySpacing = 20f;

        [Tooltip("NameArea / PhraseArea 内側の余白（px）。")]
        [SerializeField] private int _contentPadding = 16;

        /// <summary>テキストに適用するフォント。未設定なら TMP の既定フォントを使う。</summary>
        public TMP_FontAsset FontAsset => _fontAsset;

        /// <summary>話者名テキストのフォントサイズ。</summary>
        public float SpeakerFontSize => _speakerFontSize;

        /// <summary>本文テキストのフォントサイズ。</summary>
        public float BodyFontSize => _bodyFontSize;

        /// <summary>話者名テキストの色。</summary>
        public Color SpeakerColor => _speakerColor;

        /// <summary>本文テキストの色。</summary>
        public Color BodyColor => _bodyColor;

        /// <summary>ログ1件ごとの縦間隔（px）。</summary>
        public float EntrySpacing => _entrySpacing;

        /// <summary>NameArea / PhraseArea 内側の余白（px）。</summary>
        public int ContentPadding => _contentPadding;
    }
}
