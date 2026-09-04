using System;
using UnityEngine;

namespace EchoEdge.Domain.Scenario
{
    /// <summary>
    /// キャラクターが喋るセリフを表示するイベント。
    /// 話者名は、指定した位置にいる <see cref="CharacterData"/> の表示名を使用する。
    /// </summary>
    [Serializable]
    public class Phrase : IScenarioEvent
    {
        [SerializeField, Tooltip("どの位置のキャラが喋るのか")]
        private CharacterPosition _charaPosition;

        [SerializeField, TextArea, Tooltip("セリフ本文")]
        private string _text;

        public string Text => _text;
        public CharacterPosition CharaPosition => _charaPosition;
    }
}
