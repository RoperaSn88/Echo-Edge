using System;

public static class EnemyPhaseStartActionDispatcher
{
    private static event Action OnEnemyPhaseStart;

    public static void Register(IEnemyPhaseStartAction action)
    {
        if (action == null)
        {
            return;
        }

        OnEnemyPhaseStart += action.OnEnemyPhaseStart;
    }

    public static void ExecuteEnemyPhaseStartActions()
    {
        OnEnemyPhaseStart?.Invoke();
    }
}
