using System;
using UnityEngine;

namespace Domain.Scenario
{
    /// <summary>
    /// 背景を変更するイベント。
    /// </summary>
    [Serializable]
    public class BackgroundChangeEvent : IScenarioEvent
    {
        [SerializeField, Tooltip("変更先の背景")]
        private Sprite _background;

        public Sprite Background => _background;
    }
}
