using UnityEngine;

namespace Domain.Scenario
{
    /// <summary>
    /// シナリオに登場するキャラクターの定義データ。
    /// </summary>
    [CreateAssetMenu(menuName = "Scenario/CharacterData", fileName = "NewCharacterData")]
    public class CharacterData : ScriptableObject
    {
        [SerializeField, Tooltip("キャラクターを識別するID")]
        private string _characterId;

        [SerializeField, Tooltip("表示名")]
        private string _displayName;

        [SerializeField] private Sprite _normalSprite;
        [SerializeField] private Sprite _delightSprite;
        [SerializeField] private Sprite _sadSprite;
        [SerializeField] private Sprite _angrySprite;

        public string CharacterId => _characterId;
        public string DisplayName => _displayName;

        public Sprite GetSprite(EmotionType emotion)
        {
            return emotion switch
            {
                EmotionType.Normal => _normalSprite,
                EmotionType.Delight => _delightSprite,
                EmotionType.Sad => _sadSprite,
                EmotionType.Angry => _angrySprite,
                _ => _normalSprite
            };
        }
    }
}
