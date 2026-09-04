using System;
using UnityEngine;
using UnityEngine.Scripting.APIUpdating;

namespace EchoEdge.Domain.Scenario
{
    /// <summary>
    /// 指定した効果音（SE）を一度だけ再生するイベント。
    /// </summary>
    [Serializable]
    [MovedFrom(true, sourceNamespace: "Domain.Scenario")]
    public class SePlayEvent : IScenarioEvent
    {
        [SerializeField, Tooltip("再生する SE")]
        private AudioClip _clip;

        public AudioClip Clip => _clip;
    }
}
