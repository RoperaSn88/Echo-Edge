using UnityEngine;

namespace EchoEdge.Presenter.UI
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
        
        [SerializeField, Tooltip("武器のスプライト")]
        private Sprite _weaponSprite;
        public Sprite WeaponSprite => _weaponSprite;

        [SerializeField, Tooltip("起動時のプレイヤーアニメーション番号（Animator の WeaponInteger。0 はデフォルトのアニメーション）")]
        private int _animationNumber;
        public int AnimationNumber => _animationNumber;
    }
}
