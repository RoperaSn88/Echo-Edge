using System;
using UnityEngine;
using UnityEngine.Scripting.APIUpdating;

namespace EchoEdge.Domain.Scenario
{
    /// <summary>
    /// キャラクターがシナリオ上に登場するイベント。
    /// </summary>
    [Serializable]
    [MovedFrom(true, sourceNamespace: "Domain.Scenario")]
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
