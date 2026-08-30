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

        [SerializeField, Tooltip("このイベントと同時に背景を変更する場合、変更先の背景。未設定の場合は背景を変更しない")]
        private Sprite _background;

        public CharacterData Character => _character;
        public CharacterPosition Position => _position;
        public EmotionType Emotion => _emotion;
        public Sprite Background => _background;
    }
}
