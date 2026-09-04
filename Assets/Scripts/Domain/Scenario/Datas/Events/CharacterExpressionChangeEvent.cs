using System;
using UnityEngine;
using UnityEngine.Scripting.APIUpdating;

namespace EchoEdge.Domain.Scenario
{
    /// <summary>
    /// 指定した位置にいるキャラクターの表情を変更するイベント。
    /// キャラクター自体はその位置に登場済みの <see cref="CharacterData"/> を参照する。
    /// </summary>
    [Serializable]
    [MovedFrom(true, sourceNamespace: "Domain.Scenario")]
    public class CharacterExpressionChangeEvent : IScenarioEvent
    {
        [SerializeField, Tooltip("どの位置のキャラの表情を変更するのか")]
        private CharacterPosition _position;

        [SerializeField] private EmotionType _emotion;

        public CharacterPosition Position => _position;
        public EmotionType Emotion => _emotion;
    }
}
