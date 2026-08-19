using System.Collections.Generic;
using UnityEngine;

namespace Domain.Scenario
{
    /// <summary>
    /// 1つの場面を構成するシナリオイベントの並び。
    /// </summary>
    [CreateAssetMenu(menuName = "Scenario/ScenarioData", fileName = "NewScenarioData")]
    public class ScenarioData : ScriptableObject
    {
        [SerializeReference]
        private List<IScenarioEvent> _events = new();

        public List<IScenarioEvent> Events => _events;
    }
}
