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

        [SerializeField, Range(0f, 1f), Tooltip("再生音量")]
        private float _volume = 1f;

        public AudioClip Clip => _clip;
        public float Volume => _volume;
    }
}
