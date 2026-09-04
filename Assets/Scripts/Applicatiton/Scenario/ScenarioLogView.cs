using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

using EchoEdge.Domain.Scenario;

namespace EchoEdge.App.Scenario
{
    /// <summary>
    /// これまでに表示したセリフのログを ScrollView に一覧表示するパネル。
    /// 表示することに専念し、ログの中身は <see cref="ScenarioViewController"/> から受け取る。
    /// 話者名は NameArea、本文は PhraseArea の子として2列で積み上げて表示し、
    /// 件数が増えても ScrollView でスクロールして閲覧できる。
    /// 見た目の調整項目は <see cref="ScenarioLogViewSettings"/> に分離している。
    /// </summary>
    public class ScenarioLogView : MonoBehaviour
    {
        // 新しいログ行の本文末尾に付ける空行。行同士の区切りを見やすくする。
        private const string TrailingBlankLine = "\n";

        [Header("参照")]
        [SerializeField] private ScrollRect _scrollRect;
        [SerializeField] private RectTransform _content;
        [SerializeField] private RectTransform _nameArea;
        [SerializeField] private RectTransform _phraseArea;
        [SerializeField] private Button _closeButton;

        [Header("表示設定")]
        [SerializeField] private ScenarioLogViewSettings _settings = new();

        // 生成済みの行セル。name と phrase は同じインデックスで対応する。
        private readonly List<RectTransform> _nameCells = new();
        private readonly List<RectTransform> _phraseCells = new();

        // すでに描画したログ件数。次回 Refresh でここから先だけを追加する。
        private int _renderedCount;

        private bool _areaLayoutReady;

        // 必要な参照がすべて設定されているか。未設定時は表示を行わない。
        private bool _hasRequiredReferences;

        // 表示・非表示は GameObject の Active ではなく CanvasGroup で切り替える。
        // 非表示中もログ行の生成とレイアウトを進めておき、開いた瞬間に処理が集中して
        // カクつくのを防ぐ。
        private CanvasGroup _canvasGroup;

        // ログパネルが開いているか。CanvasGroup の表示状態と一致する。
        private bool _isOpen;

        /// <summary>
        /// 表示状態が変化した際に発火する。引数は表示中かどうか。
        /// </summary>
        public event Action<bool> VisibilityChanged;

        /// <summary>
        /// ログパネルが現在表示されているかどうか。
        /// </summary>
        public bool IsOpen => _isOpen;

        private void Awake()
        {
            _hasRequiredReferences = ValidateReferences();
            if (!_hasRequiredReferences)
            {
                return;
            }

            _canvasGroup = GetComponent<CanvasGroup>();
            if (_canvasGroup == null)
            {
                _canvasGroup = gameObject.AddComponent<CanvasGroup>();
            }

            _closeButton.onClick.AddListener(Hide);
            EnsureAreaLayout();
            ApplyVisibility(false);
        }

        /// <summary>
        /// 別シナリオ開始などで（親ごと）再アクティブ化された際は、ログを閉じた状態から始める。
        /// </summary>
        private void OnEnable()
        {
            if (_canvasGroup != null)
            {
                ApplyVisibility(false);
            }
        }

        /// <summary>
        /// 必要なシーン参照がそろっているか検証する。欠けている場合はエラーを出してこのコンポーネントを無効化する。
        /// シーン参照の付け替え直後などに NullReferenceException で落ちるのを防ぎ、原因を特定しやすくする。
        /// </summary>
        private bool ValidateReferences()
        {
            if (_scrollRect != null && _content != null && _nameArea != null && _phraseArea != null && _closeButton != null)
            {
                return true;
            }

            Debug.LogError(
                $"{nameof(ScenarioLogView)}: 参照（ScrollRect / Content / NameArea / PhraseArea / CloseButton）が未設定のため、ログ表示を無効化します。",
                this);
            enabled = false;
            return false;
        }

        /// <summary>
        /// ログパネルを表示する。表示時は末尾（最新）までスクロールする。
        /// </summary>
        public void Show()
        {
            ApplyVisibility(true);
            ScrollToBottom();
            VisibilityChanged?.Invoke(true);
        }

        /// <summary>
        /// ログパネルを隠す。
        /// </summary>
        public void Hide()
        {
            ApplyVisibility(false);
            VisibilityChanged?.Invoke(false);
        }

        /// <summary>
        /// 表示・非表示を切り替える。
        /// </summary>
        public void Toggle()
        {
            if (_isOpen)
            {
                Hide();
            }
            else
            {
                Show();
            }
        }

        /// <summary>
        /// CanvasGroup で表示・非表示を切り替える。GameObject は常にアクティブのままにして、
        /// 非表示中もログ行の生成とレイアウト・TMP のメッシュ生成を進められるようにする。
        /// </summary>
        private void ApplyVisibility(bool visible)
        {
            _isOpen = visible;

            if (_canvasGroup == null)
            {
                return;
            }

            _canvasGroup.alpha = visible ? 1f : 0f;
            _canvasGroup.interactable = visible;
            _canvasGroup.blocksRaycasts = visible;
        }

        /// <summary>
        /// ログの内容を最新の状態に描画し直す。
        /// 直前の描画から増えた分だけを追加し、ログがリセットされた場合は全消去して作り直す。
        /// </summary>
        public void Refresh(IReadOnlyList<ScenarioLogEntry> entries)
        {
            if (!_hasRequiredReferences)
            {
                return;
            }

            EnsureAreaLayout();

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

            SyncRowHeights();
            RebuildContentSize();

            // 非表示中はスクロール位置を触らない。開いたときに Show() が末尾へ寄せる。
            if (_isOpen)
            {
                ScrollToBottom();
            }
        }

        /// <summary>
        /// NameArea / PhraseArea に縦積みレイアウトと高さ自動調整を用意する。シーン側で付け忘れても動くようにする。
        /// </summary>
        private void EnsureAreaLayout()
        {
            if (_areaLayoutReady || _nameArea == null || _phraseArea == null) return;

            SetupColumn(_nameArea);
            SetupColumn(_phraseArea);

            _areaLayoutReady = true;
        }

        private void SetupColumn(RectTransform column)
        {
            var layout = column.GetComponent<VerticalLayoutGroup>();
            if (layout == null)
            {
                layout = column.gameObject.AddComponent<VerticalLayoutGroup>();
            }

            var padding = _settings.ContentPadding;
            layout.padding = new RectOffset(padding, padding, padding, padding);
            layout.spacing = _settings.EntrySpacing;
            layout.childAlignment = TextAnchor.UpperLeft;
            layout.childControlWidth = true;
            layout.childControlHeight = true;
            layout.childForceExpandWidth = true;
            layout.childForceExpandHeight = false;

            var fitter = column.GetComponent<ContentSizeFitter>();
            if (fitter == null)
            {
                fitter = column.gameObject.AddComponent<ContentSizeFitter>();
            }

            fitter.horizontalFit = ContentSizeFitter.FitMode.Unconstrained;
            fitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;
        }

        /// <summary>
        /// ログ1件分のセルを、話者名は NameArea、本文は PhraseArea の子として生成する。
        /// 地の文（話者名なし）でも行の対応がずれないよう、空の話者名セルを必ず作る。
        /// </summary>
        private void CreateEntry(ScenarioLogEntry entry)
        {
            var speakerName = entry.SpeakerName ?? string.Empty;
            var nameCell = CreateText(_nameArea, speakerName, _settings.SpeakerFontSize, _settings.SpeakerColor, FontStyles.Bold, HorizontalAlignmentOptions.Left);
            nameCell.gameObject.AddComponent<LayoutElement>();

            var phraseCell = CreateText(_phraseArea, entry.Text + TrailingBlankLine, _settings.BodyFontSize, _settings.BodyColor, FontStyles.Normal, HorizontalAlignmentOptions.Left);

            _nameCells.Add(nameCell);
            _phraseCells.Add(phraseCell);
        }

        private RectTransform CreateText(Transform parent, string value, float fontSize, Color color, FontStyles style, HorizontalAlignmentOptions horizontalAlignment)
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
            text.horizontalAlignment = horizontalAlignment;
            text.verticalAlignment = VerticalAlignmentOptions.Top;

            return (RectTransform)textObject.transform;
        }

        /// <summary>
        /// 各行について、話者名セルの高さを対応する本文セルの高さに合わせ、2列の行位置をそろえる。
        /// </summary>
        private void SyncRowHeights()
        {
            if (_phraseCells.Count == 0) return;

            Canvas.ForceUpdateCanvases();
            LayoutRebuilder.ForceRebuildLayoutImmediate(_phraseArea);

            for (var i = 0; i < _nameCells.Count && i < _phraseCells.Count; i++)
            {
                var layoutElement = _nameCells[i].GetComponent<LayoutElement>();
                if (layoutElement != null)
                {
                    layoutElement.minHeight = _phraseCells[i].rect.height;
                }
            }

            LayoutRebuilder.ForceRebuildLayoutImmediate(_nameArea);
        }

        /// <summary>
        /// Content の高さを NameArea / PhraseArea のうち高い方に合わせ、スクロール範囲を確定させる。
        /// </summary>
        private void RebuildContentSize()
        {
            Canvas.ForceUpdateCanvases();
            LayoutRebuilder.ForceRebuildLayoutImmediate(_nameArea);
            LayoutRebuilder.ForceRebuildLayoutImmediate(_phraseArea);

            var height = Mathf.Max(_nameArea.rect.height, _phraseArea.rect.height);
            _content.SetSizeWithCurrentAnchors(RectTransform.Axis.Vertical, height);
        }

        /// <summary>
        /// 生成済みの行セルをすべて破棄し、描画済み件数をリセットする。
        /// Destroy は実行フレーム末尾まで反映されないため、先に親から外して同フレームの
        /// レイアウト再計算に混ざらないようにする。
        /// </summary>
        private void ClearEntries()
        {
            DestroyCells(_nameCells);
            DestroyCells(_phraseCells);
            _renderedCount = 0;
        }

        private static void DestroyCells(List<RectTransform> cells)
        {
            foreach (var cell in cells)
            {
                if (cell == null) continue;

                cell.SetParent(null, false);
                cell.gameObject.SetActive(false);
                Destroy(cell.gameObject);
            }

            cells.Clear();
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
