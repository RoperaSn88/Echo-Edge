using System;
using UnityEngine;

namespace Domain.Scenario
{
    /// <summary>
    /// 指定した BGM をループ再生するイベント。
    /// 既に同じ曲が再生中の場合は再生し直さない。
    /// </summary>
    [Serializable]
    public class BgmPlayEvent : IScenarioEvent
    {
        [SerializeField, Tooltip("再生する BGM")]
        private AudioClip _clip;

        [SerializeField, Range(0f, 1f), Tooltip("再生音量")]
        private float _volume = 1f;

        public AudioClip Clip => _clip;
        public float Volume => _volume;
    }
}
