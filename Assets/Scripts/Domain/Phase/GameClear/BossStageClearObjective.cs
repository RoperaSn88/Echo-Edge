using System;

/// <summary>
/// ボスのクリア条件。特定の敵(ボス)を撃破するとクリアとなる。
/// </summary>
public sealed class BossStageClearObjective : IStageClearObjective
{
    private const string BaseMessage = "ボスを倒す";
    private EnemyKinds _targetEnemyKind;
    private bool _isBossDefeated;

    public event Func<bool> OnGameClearInteraction;

    public string ObjectiveBaseText => BaseMessage;

    public string ObjectiveConditionValue => _isBossDefeated ? "撃破" : "未撃破";

    /// <summary>
    /// <paramref name="targetEnemyKindId"/> には撃破対象となる <see cref="EnemyKinds"/> の値を渡す。
    /// </summary>
    public void Initialize(int targetEnemyKindId)
    {
        _targetEnemyKind = (EnemyKinds)targetEnemyKindId;
        _isBossDefeated = false;
    }

    /// <summary>
    /// <paramref name="progressValue"/> には撃破された敵の <see cref="EnemyKinds"/> を渡す。
    /// </summary>
    public void UpdateCondition(int progressValue = 0)
    {
        if ((EnemyKinds)progressValue == _targetEnemyKind)
        {
            _isBossDefeated = true;
        }

        if (IsGameClearCondition())
        {
            GameClearInteraction();
        }
    }

    public bool IsGameClearCondition()
    {
        return _isBossDefeated;
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
