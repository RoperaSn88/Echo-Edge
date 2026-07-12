using System;

/// <summary>
/// 耐久型のクリア条件。開始からkターン経過するまでプレイヤーが生存していればクリアとなる。
/// </summary>
public sealed class EnduranceStageClearObjective : IStageClearObjective
{
    private const string BaseMessage = "生存まであと";
    private int _remainingTurns;

    public event Func<bool> OnGameClearInteraction;

    public string ObjectiveBaseText => BaseMessage;

    public string ObjectiveConditionValue => _remainingTurns.ToString();

    public void Initialize(int requiredTurns)
    {
        _remainingTurns = Math.Max(0, requiredTurns);
    }

    public void UpdateCondition(int progressValue = 0)
    {
        if (_remainingTurns > 0)
        {
            _remainingTurns--;
        }

        if (IsGameClearCondition())
        {
            GameClearInteraction();
        }
    }

    public bool IsGameClearCondition()
    {
        return _remainingTurns <= 0;
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
