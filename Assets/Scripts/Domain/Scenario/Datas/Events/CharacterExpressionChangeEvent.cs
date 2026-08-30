using System;
using UnityEngine;

namespace Domain.Scenario
{
    /// <summary>
    /// 指定した位置にいるキャラクターの表情を変更するイベント。
    /// キャラクター自体はその位置に登場済みの <see cref="CharacterData"/> を参照する。
    /// </summary>
    [Serializable]
    public class CharacterExpressionChangeEvent : IScenarioEvent
    {
        [SerializeField, Tooltip("どの位置のキャラの表情を変更するのか")]
        private CharacterPosition _position;

        [SerializeField] private EmotionType _emotion;

        [SerializeField, Tooltip("このイベントと同時に背景を変更する場合、変更先の背景。未設定の場合は背景を変更しない")]
        private Sprite _background;

        public CharacterPosition Position => _position;
        public EmotionType Emotion => _emotion;
        public Sprite Background => _background;
    }
}
