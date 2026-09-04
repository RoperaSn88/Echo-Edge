using System;
using UnityEngine;
using UnityEngine.Scripting.APIUpdating;

namespace EchoEdge.Domain.Scenario
{
    /// <summary>
    /// 背景を変更するイベント。
    /// </summary>
    [Serializable]
    [MovedFrom(true, sourceNamespace: "Domain.Scenario")]
    public class BackgroundChangeEvent : IScenarioEvent
    {
        [SerializeField, Tooltip("変更先の背景")]
        private Sprite _background;

        public Sprite Background => _background;
    }
}
