using Cysharp.Threading.Tasks;

namespace EchoEdge.Domain.Battle
{
    /// <summary>
    /// 他ユニットのスキルコストとして犠牲になれるユニットのインターフェース。
    /// 「犠牲にできるか」を具象クラスではなく型で判定できるようにする。
    /// </summary>
    public interface ISacrificable
    {
        /// <summary>
        /// 自身を犠牲にする。
        /// 防御力・無敵状態を無視して確実に死亡し、経験値・エナジーの撃破報酬は発生しない。
        /// </summary>
        UniTask Sacrifice();
    }
}
