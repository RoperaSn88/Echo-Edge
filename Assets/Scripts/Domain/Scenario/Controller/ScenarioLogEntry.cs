namespace Domain.Scenario.Controller
{
    /// <summary>
    /// ログに表示するセリフ1件分のデータ。
    /// </summary>
    public readonly struct ScenarioLogEntry
    {
        public string SpeakerName { get; }
        public string Text { get; }

        public ScenarioLogEntry(string speakerName, string text)
        {
            SpeakerName = speakerName;
            Text = text;
        }
    }
}
