using System.Collections.Generic;
using Domain.Scenario.Controller;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Applicatiton.Scenario
{
    /// <summary>
    /// これまでに表示したセリフのログを ScrollView に一覧表示するパネル。
    /// 表示することに専念し、ログの中身は <see cref="ScenarioViewController"/> から受け取る。
    /// ログ1件につき「キャラクター名」と「本文テキスト」を積み上げて表示し、
    /// 件数が増えても ScrollView でスクロールして閲覧できる。
    /// 見た目の調整項目は <see cref="ScenarioLogViewSettings"/> に分離している。
    /// </summary>
    public class ScenarioLogView : MonoBehaviour
    {
        [Header("参照")]
        [SerializeField] private ScrollRect _scrollRect;
        [SerializeField] private RectTransform _content;
        [SerializeField] private Button _closeButton;

        [Header("表示設定")]
        [SerializeField] private ScenarioLogViewSettings _settings = new();

        // 生成済みのログ行 GameObject。差分更新のために保持する。
        private readonly List<GameObject> _entryObjects = new();

        // すでに描画したログ件数。次回 Refresh でここから先だけを追加する。
        private int _renderedCount;

        private bool _layoutReady;

        // 必要な参照がすべて設定されているか。未設定時は表示を行わない。
        private bool _hasRequiredReferences;

        private void Awake()
        {
            _hasRequiredReferences = ValidateReferences();
            if (!_hasRequiredReferences)
            {
                return;
            }

            _closeButton.onClick.AddListener(Hide);
            EnsureContentLayout();
        }

        /// <summary>
        /// 必要なシーン参照がそろっているか検証する。欠けている場合はエラーを出してこのコンポーネントを無効化する。
        /// シーン参照の付け替え直後などに NullReferenceException で落ちるのを防ぎ、原因を特定しやすくする。
        /// </summary>
        private bool ValidateReferences()
        {
            if (_scrollRect != null && _content != null && _closeButton != null)
            {
                return true;
            }

            Debug.LogError(
                $"{nameof(ScenarioLogView)}: 参照（ScrollRect / Content / CloseButton）が未設定のため、ログ表示を無効化します。",
                this);
            enabled = false;
            return false;
        }

        /// <summary>
        /// ログパネルを表示する。表示時は末尾（最新）までスクロールする。
        /// </summary>
        public void Show()
        {
            gameObject.SetActive(true);
            ScrollToBottom();
        }

        /// <summary>
        /// ログパネルを隠す。
        /// </summary>
        public void Hide()
        {
            gameObject.SetActive(false);
        }

        /// <summary>
        /// 表示・非表示を切り替える。
        /// </summary>
        public void Toggle()
        {
            if (gameObject.activeSelf)
            {
                Hide();
            }
            else
            {
                Show();
            }
        }

        /// <summary>
        /// ログの内容を最新の状態に描画し直す。
        /// 直前の描画から増えた分だけを Content に追加し、ログがリセットされた場合は全消去して作り直す。
        /// </summary>
        public void Refresh(IReadOnlyList<ScenarioLogEntry> entries)
        {
            if (!_hasRequiredReferences)
            {
                return;
            }

            EnsureContentLayout();

            // ログが減った・空になった（別シナリオ開始など）場合は作り直す。
            if (entries.Count < _renderedCount)
            {
                ClearEntries();
            }

            for (var i = _renderedCount; i < entries.Count; i++)
            {
                CreateEntry(entries[i]);
            }

            _renderedCount = entries.Count;

            if (isActiveAndEnabled)
            {
                ScrollToBottom();
            }
        }

        /// <summary>
        /// Content に縦積みレイアウトと高さ自動調整を用意する。シーン側で付け忘れても動くようにする。
        /// </summary>
        private void EnsureContentLayout()
        {
            if (_layoutReady || _content == null) return;

            var layout = _content.GetComponent<VerticalLayoutGroup>();
            if (layout == null)
            {
                layout = _content.gameObject.AddComponent<VerticalLayoutGroup>();
            }

            var padding = _settings.ContentPadding;
            layout.padding = new RectOffset(padding, padding, padding, padding);
            layout.spacing = _settings.EntrySpacing;
            layout.childAlignment = TextAnchor.UpperLeft;
            layout.childControlWidth = true;
            layout.childControlHeight = true;
            layout.childForceExpandWidth = true;
            layout.childForceExpandHeight = false;

            var fitter = _content.GetComponent<ContentSizeFitter>();
            if (fitter == null)
            {
                fitter = _content.gameObject.AddComponent<ContentSizeFitter>();
            }

            fitter.horizontalFit = ContentSizeFitter.FitMode.Unconstrained;
            fitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;

            _layoutReady = true;
        }

        /// <summary>
        /// ログ1件分の GameObject（話者名テキスト＋本文テキスト）を生成して Content に追加する。
        /// </summary>
        private void CreateEntry(ScenarioLogEntry entry)
        {
            var entryObject = new GameObject("LogEntry", typeof(RectTransform));
            entryObject.transform.SetParent(_content, false);

            var entryLayout = entryObject.AddComponent<VerticalLayoutGroup>();
            entryLayout.spacing = _settings.SpeakerBodySpacing;
            entryLayout.childAlignment = TextAnchor.UpperLeft;
            entryLayout.childControlWidth = true;
            entryLayout.childControlHeight = true;
            entryLayout.childForceExpandWidth = true;
            entryLayout.childForceExpandHeight = false;

            if (!string.IsNullOrEmpty(entry.SpeakerName))
            {
                CreateText(entryObject.transform, entry.SpeakerName, _settings.SpeakerFontSize, _settings.SpeakerColor, FontStyles.Bold);
            }

            CreateText(entryObject.transform, entry.Text, _settings.BodyFontSize, _settings.BodyColor, FontStyles.Normal);

            _entryObjects.Add(entryObject);
        }

        private void CreateText(Transform parent, string value, float fontSize, Color color, FontStyles style)
        {
            var textObject = new GameObject("Text", typeof(RectTransform));
            textObject.transform.SetParent(parent, false);

            var text = textObject.AddComponent<TextMeshProUGUI>();
            if (_settings.FontAsset != null)
            {
                text.font = _settings.FontAsset;
            }

            text.text = value;
            text.fontSize = fontSize;
            text.color = color;
            text.fontStyle = style;
            text.textWrappingMode = TextWrappingModes.Normal;
            text.raycastTarget = false;
            text.richText = true;
        }

        /// <summary>
        /// 生成済みのログ行をすべて破棄し、描画済み件数をリセットする。
        /// Destroy は実行フレーム末尾まで反映されないため、先に親から外して同フレームの
        /// レイアウト再計算（<see cref="ScrollToBottom"/>）に混ざらないようにする。
        /// </summary>
        private void ClearEntries()
        {
            foreach (var entryObject in _entryObjects)
            {
                if (entryObject == null) continue;

                entryObject.transform.SetParent(null, false);
                entryObject.SetActive(false);
                Destroy(entryObject);
            }

            _entryObjects.Clear();
            _renderedCount = 0;
        }

        /// <summary>
        /// レイアウトを確定させたうえで最新のログ（末尾）までスクロールする。
        /// </summary>
        private void ScrollToBottom()
        {
            if (_scrollRect == null || _content == null) return;

            Canvas.ForceUpdateCanvases();
            LayoutRebuilder.ForceRebuildLayoutImmediate(_content);
            _scrollRect.verticalNormalizedPosition = 0f;
        }
    }
}
