using Cysharp.Threading.Tasks;

using EchoEdge.App.Battle;

namespace EchoEdge.Domain.Battle
{
    /// <summary>
    /// リフレクトチャージャーの効果クラス
    /// このターンの間、プレイヤーの反射回数を増やすバフを付ける
    /// </summary>
    public class ReflectCharger : IInstantEquipEffect
    {
        /// <summary>
        /// バフの効果ターン数。プレイヤーのターン終了時の TickBuffs で消える
        /// </summary>
        private const int DurationTurns = 1;

        /// <summary>
        /// リフレクトチャージャーの効果を発揮する
        /// </summary>
        public UniTask Activate()
        {
            // プレイヤーの反射回数は BattleStatus.Move を使っている
            BattleManager.PlayerStatus?.AddBuff(new MoveBuff(), DurationTurns);
            return UniTask.CompletedTask;
        }
    }
}
