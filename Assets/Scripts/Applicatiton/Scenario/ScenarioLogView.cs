using System.Collections.Generic;
using System.Text;
using Domain.Scenario.Controller;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Applicatiton.Scenario
{
    /// <summary>
    /// これまでに表示したセリフのログを一覧表示するパネル。
    /// 表示することに専念し、ログの中身は <see cref="ScenarioViewController"/> から受け取る。
    /// </summary>
    public class ScenarioLogView : MonoBehaviour
    {
        // スクロール機能を持たないため、直近この件数分のみを表示して溢れを防ぐ。
        private const int MaxDisplayCount = 12;

        [SerializeField] private TextMeshProUGUI _logText;
        [SerializeField] private Button _closeButton;

        private void Awake()
        {
            _closeButton.onClick.AddListener(Hide);
        }

        /// <summary>
        /// ログパネルを表示する。
        /// </summary>
        public void Show()
        {
            gameObject.SetActive(true);
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
        /// スクロール機能を持たないため、直近 <see cref="MaxDisplayCount"/> 件のみを表示する。
        /// </summary>
        public void Refresh(IReadOnlyList<ScenarioLogEntry> entries)
        {
            var startIndex = Mathf.Max(0, entries.Count - MaxDisplayCount);

            var builder = new StringBuilder();
            for (var i = startIndex; i < entries.Count; i++)
            {
                var entry = entries[i];
                if (!string.IsNullOrEmpty(entry.SpeakerName))
                {
                    builder.Append("<b>").Append(entry.SpeakerName).Append("</b>\n");
                }

                builder.Append(entry.Text).Append("\n\n");
            }

            _logText.text = builder.ToString();
        }
    }
}
