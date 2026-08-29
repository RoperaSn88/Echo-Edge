using System;
using UnityEngine;

namespace Domain.Scenario
{
    /// <summary>
    /// 指定した効果音（SE）を一度だけ再生するイベント。
    /// </summary>
    [Serializable]
    public class SePlayEvent : IScenarioEvent
    {
        [SerializeField, Tooltip("再生する SE")]
        private AudioClip _clip;

        public AudioClip Clip => _clip;
    }
}
