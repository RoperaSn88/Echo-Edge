using Cysharp.Threading.Tasks;
using UnityEngine;

namespace EchoEdge.Domain.Battle
{
    /// <summary>
    /// 『エナー』専用のユニット。
    /// 仲間のエナーに吸収される（＝犠牲になる）という固有の死に方を持つ。
    /// ターン中の行動手順（吸収対象の抽選や1ターン1回の制限）は Enar(IUnitAction) 側の責務。
    /// </summary>
    public class EnarUnit : BaseUnit, ISacrificable
    {
        public EnarUnit(int h, int w, EnemySize size = EnemySize.Default) : base(h, w, size)
        {
        }

        /// <inheritdoc/>
        public async UniTask Sacrifice()
        {
            var status = GetStatus();
            if (status == null)
            {
                Debug.LogWarning("ステータスが読み込まれていないため、犠牲にできません。");
                return;
            }

            // 現在HP分を消費させることで、防御力・無敵状態に関係なく確実に死亡させる
            // （MaxHP ではなく現在HPを渡すことで、表示されるダメージ量が実際に失ったHPと一致する）
            var result = await status.ConsumeHP(status.HP);

            // 吸収による死のためエナジーは発生させない
            await ReflectDamageToView(result, showEnergy: false);

            if (result.isDeath)
            {
                // 撃破報酬ではないため経験値も発生させない
                await Dead(experienceReward: 0);
            }
        }
    }
}
