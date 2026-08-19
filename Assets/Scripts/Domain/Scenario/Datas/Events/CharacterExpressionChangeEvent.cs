using System;
using UnityEngine;

namespace Domain.Scenario
{
    /// <summary>
    /// キャラクターの表情を変更するイベント。
    /// </summary>
    [Serializable]
    public class CharacterExpressionChangeEvent : IScenarioEvent
    {
        [SerializeField] private CharacterData _character;
        [SerializeField] private EmotionType _emotion;

        public CharacterData Character => _character;
        public EmotionType Emotion => _emotion;
    }
}
