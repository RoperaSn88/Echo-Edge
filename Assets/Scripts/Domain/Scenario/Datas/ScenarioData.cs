using System;
using System.Collections.Generic;
using UnityEngine;

namespace Domain.Scenario
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

        [SerializeField]
        private List<EventRow> _rows = new();

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
