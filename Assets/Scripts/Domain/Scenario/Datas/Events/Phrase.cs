using System;
using UnityEngine;

namespace Domain.Scenario
{
    /// <summary>
    /// キャラクターが喋るセリフを表示するイベント。
    /// </summary>
    [Serializable]
    public class Phrase : IScenarioEvent
    {
        [SerializeField, Tooltip("どの位置のキャラが喋るのか")]
        private CharacterPosition _charaPosition;
        
        [SerializeField, Tooltip("話者名などの表示用テキスト")]
        private string _charaText;

        [SerializeField, TextArea, Tooltip("セリフ本文")]
        private string _text;

        public string CharaText => _charaText;
        public string Text => _text;
        public CharacterPosition CharaPosition => _charaPosition;
    }
}
