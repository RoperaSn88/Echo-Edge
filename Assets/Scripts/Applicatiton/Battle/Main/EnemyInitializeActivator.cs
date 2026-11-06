using System;

using EchoEdge.Domain.Battle;

namespace EchoEdge.App.Battle
{
    /// <summary>
    /// 敵のステータス初期化時に、登録された <see cref="IEnemyInitializeAction"/> を呼び出す
    /// </summary>
    public static class EnemyInitializeActivator
    {
        private static event Action<EnemyKinds, BattleStatus> OnEnemyInitialize;

        public static void Register(IEnemyInitializeAction action)
        {
            if (action == null)
            {
                return;
            }

            OnEnemyInitialize -= action.OnEnemyInitialize;
            OnEnemyInitialize += action.OnEnemyInitialize;
        }

        public static void Unregister(IEnemyInitializeAction action)
        {
            if (action == null)
            {
                return;
            }

            OnEnemyInitialize -= action.OnEnemyInitialize;
        }

        public static void ExecuteEnemyInitializeActions(EnemyKinds enemyKind, BattleStatus status)
        {
            OnEnemyInitialize?.Invoke(enemyKind, status);
        }
    }
}
