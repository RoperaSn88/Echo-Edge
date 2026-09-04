using System;
using UnityEngine;
using UnityEngine.Scripting.APIUpdating;

namespace EchoEdge.Domain.Scenario
{
    /// <summary>
    /// BGM の再生・停止を行うイベント。
    /// </summary>
    [Serializable]
    [MovedFrom(true, sourceNamespace: "Domain.Scenario")]
    public class BgmEvent : IScenarioEvent
    {
        [SerializeField, Tooltip("再生するか停止するか")]
        private BgmEventAction _action;

        [SerializeField, Tooltip("再生する BGM（再生時のみ使用）")]
        private AudioClip _bgm;

        [SerializeField, Tooltip("ループ再生するか（再生時のみ使用）")]
        private bool _isLoop;

        [SerializeField, Tooltip("フェードインで再生するか、即時再生するか（再生時のみ使用）")]
        private bool _isFadeIn;

        [SerializeField, Tooltip("フェードアウトで停止するか、即時停止するか（停止時のみ使用）")]
        private bool _isFadeOut;

        public BgmEventAction Action => _action;
        public AudioClip Bgm => _bgm;
        public bool IsLoop => _isLoop;
        public bool IsFadeIn => _isFadeIn;
        public bool IsFadeOut => _isFadeOut;
    }
}
