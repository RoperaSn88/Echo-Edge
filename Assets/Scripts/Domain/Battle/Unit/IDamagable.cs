using Cysharp.Threading.Tasks;

namespace EchoEdge.Domain.Battle
{
    public interface IDamagable
    {
        /// <summary>
        /// ダメージを受けるためのメソッド
        /// </summary>
        public UniTask<(int damage, bool isDeath)> Damage(int damage);
    }
}
