using System;

/// <summary>
/// 特殊条件のクリア条件。一閃による攻撃で1度にn体の敵を撃破するとクリアとなる。
/// </summary>
public sealed class IssenMultiKillStageClearObjective : IStageClearObjective
{
    private const string BaseMessage = "一閃で同時に倒す数";
    private int _requiredKillCount;
    private int _bestKillCount;

    public event Func<bool> OnGameClearInteraction;

    public string ObjectiveBaseText => BaseMessage;

    public string ObjectiveConditionValue => $"{_bestKillCount}/{_requiredKillCount}";

    public void Initialize(int requiredKillCount)
    {
        _requiredKillCount = Math.Max(1, requiredKillCount);
        _bestKillCount = 0;
    }

    /// <summary>
    /// <paramref name="progressValue"/> には一閃1回で同時に撃破した敵の数を渡す。
    /// </summary>
    public void UpdateCondition(int progressValue = 0)
    {
        if (progressValue > _bestKillCount)
        {
            _bestKillCount = progressValue;
        }

        if (IsGameClearCondition())
        {
            GameClearInteraction();
        }
    }

    public bool IsGameClearCondition()
    {
        return _bestKillCount >= _requiredKillCount;
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
