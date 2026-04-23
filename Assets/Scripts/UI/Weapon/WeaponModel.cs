using UnityEngine;

namespace UI.Weapon
{
    [CreateAssetMenu(menuName = "Weapon/WeaponModel")]
    public class WeaponModel : ScriptableObject 
    {
        [SerializeField, Tooltip("武器のID")]
        private int _id;
        public int Id => _id;
        
        [SerializeField, Tooltip("武器の名前")]
        private string _weaponName;
        public string WeaponName => _weaponName;
        
        [SerializeField, TextArea,  Tooltip("武器の説明")]
        private string _description;
        public string Description => _description;

        [SerializeField, Tooltip("武器のコスト")]
        private int _weaponCost;
        public int WeaponCost => _weaponCost;

        [SerializeField, Tooltip("武器のHP")]
        private int _hp;
        public int HP => _hp;

        [SerializeField, Tooltip("武器の攻撃力")]
        private int _attack;
        public int Attack => _attack;
        
        [SerializeField, Tooltip("武器のスプライト")]
        private Sprite _weaponSprite;
        public Sprite WeaponSprite => _weaponSprite;
    }
}
