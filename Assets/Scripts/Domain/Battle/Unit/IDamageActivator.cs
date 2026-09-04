using Cysharp.Threading.Tasks;

namespace EchoEdge.Domain.Battle
{
    public interface IDamageActivator
    {
        public UniTask Damage(float rate = 1.0f);

        /// <summary>
        /// めちゃくちゃ早い一閃によるダメージ処理
        /// </summary>
        public UniTask FlashDamage(float rate = 1.0f);
    }
}
