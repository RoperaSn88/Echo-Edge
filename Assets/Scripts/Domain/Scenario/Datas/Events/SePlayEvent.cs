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

        [SerializeField, Tooltip("このイベントと同時に背景を変更する場合、変更先の背景。未設定の場合は背景を変更しない")]
        private Sprite _background;

        public AudioClip Clip => _clip;
        public Sprite Background => _background;
    }
}
