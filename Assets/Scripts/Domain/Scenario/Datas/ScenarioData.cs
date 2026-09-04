using System;
using System.Collections.Generic;
using UnityEngine;

namespace EchoEdge.Domain.Scenario
{
    /// <summary>
    /// 1つの場面を構成するシナリオイベントの並び。
    /// 外側の List（行）はシナリオの時間軸、内側の List（列）は同じタイミングで
    /// 同時に実行するイベント群を表す。
    /// </summary>
    /// <remarks>
    /// Unity のシリアライズシステムは List&lt;List&lt;T&gt;&gt; のようなネストしたコレクションを
    /// 直接シリアライズできないため、内側の List は <see cref="EventRow"/> でラップして保持する。
    /// 外部に公開する <see cref="Events"/> はそこから毎回組み立てるため、常に実データと一致する。
    /// </remarks>
    [CreateAssetMenu(menuName = "Scenario/ScenarioData", fileName = "NewScenarioData")]
    public class ScenarioData : ScriptableObject
    {
        /// <summary>
        /// 同じタイミングで同時に実行されるイベントのまとまり（時間軸上の1行）。
        /// </summary>
        [Serializable]
        private class EventRow
        {
            [SerializeReference] private List<IScenarioEvent> _events = new();

            public List<IScenarioEvent> Events => _events;
        }

        [SerializeField, Tooltip("Scenario シーン起動時・最初の Step が再生される前に流す BGM")]
        private AudioClip _bgm;

        [SerializeField, Tooltip("Scenario シーン起動時・最初の Step が再生される前に変更する背景。未設定の場合は背景を変更しない")]
        private Sprite _background;

        [SerializeField]
        private List<EventRow> _rows = new();

        /// <summary>
        /// このシナリオの再生中に流す BGM。割り当てられていない場合は null。
        /// </summary>
        public AudioClip Bgm => _bgm;

        /// <summary>
        /// シナリオ起動時に変更する背景。割り当てられていない場合は null。
        /// </summary>
        public Sprite Background => _background;

        /// <summary>
        /// シナリオイベントの一覧。
        /// 外側の List（行）がシナリオの時間軸、内側の List（列）が同時に実行するイベント群を表す。
        /// </summary>
        public List<List<IScenarioEvent>> Events
        {
            get
            {
                var events = new List<List<IScenarioEvent>>(_rows.Count);
                foreach (var row in _rows)
                {
                    events.Add(row.Events);
                }
                return events;
            }
        }
    }
}
