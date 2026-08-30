using System;
using UnityEngine;

namespace Domain.Scenario
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

        [SerializeField, Tooltip("このイベントと同時に背景を変更する場合、変更先の背景。未設定の場合は背景を変更しない")]
        private Sprite _background;

        public string Text => _text;
        public CharacterPosition CharaPosition => _charaPosition;
        public Sprite Background => _background;
    }
}
