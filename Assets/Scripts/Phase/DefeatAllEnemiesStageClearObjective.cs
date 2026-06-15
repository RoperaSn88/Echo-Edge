using System;

public sealed class DefeatAllEnemiesStageClearObjective : IStageClearObjective
{
    private const string BaseMessage = "残りの敵はあと";
    private int _remainingEnemyCount;

    public event Func<bool> OnGameClearInteraction;

    public string ObjectiveBaseText => BaseMessage;

    public string ObjectiveConditionValue => _remainingEnemyCount.ToString();

    public void Initialize(int enemyCount)
    {
        _remainingEnemyCount = Math.Max(0, enemyCount);
    }

    public void UpdateCondition()
    {
        if (_remainingEnemyCount > 0)
        {
            _remainingEnemyCount--;
        }

        if (IsGameClearCondition())
        {
            GameClearInteraction();
        }
    }

    public bool IsGameClearCondition()
    {
        return _remainingEnemyCount <= 0;
    }

    public bool GameClearInteraction()
    {
        if (!IsGameClearCondition())
        {
            return false;
        }

        var handlers = OnGameClearInteraction;
        if (handlers == null)
        {
            return true;
        }

        var isInvoked = false;
        foreach (Func<bool> handler in handlers.GetInvocationList())
        {
            isInvoked |= handler.Invoke();
        }

        return isInvoked;
    }
}
