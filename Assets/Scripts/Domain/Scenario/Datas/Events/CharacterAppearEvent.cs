using System;
using UnityEngine;

namespace Domain.Scenario
{
    /// <summary>
    /// キャラクターがシナリオ上に登場するイベント。
    /// </summary>
    [Serializable]
    public class CharacterAppearEvent : IScenarioEvent
    {
        [SerializeField] private CharacterData _character;
        [SerializeField] private CharacterPosition _position;
        [SerializeField] private EmotionType _emotion;

        public CharacterData Character => _character;
        public CharacterPosition Position => _position;
        public EmotionType Emotion => _emotion;
    }
}
