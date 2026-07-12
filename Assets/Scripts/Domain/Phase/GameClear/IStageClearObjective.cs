using System;

public interface IStageClearObjective
{
    /// <summary>
    /// ゲームクリア条件を満たしているかを返す。
    /// </summary>
    bool IsGameClearCondition();

    /// <summary>
    /// ゲームクリアインタラクションを実行する。
    /// </summary>
    bool GameClearInteraction();

    /// <summary>
    /// ステージ開始時に条件の初期値を設定する。
    /// </summary>
    void Initialize(int conditionValue);

    /// <summary>
    /// 条件の進捗を更新する。
    /// </summary>
    /// <param name="progressValue">
    /// 更新のきっかけとなったイベントの値（撃破した敵の種別ID、一閃での同時撃破数など）。
    /// 使用しない条件タイプでは無視される。
    /// </param>
    void UpdateCondition(int progressValue = 0);

    /// <summary>
    /// 目標表示のベーステキスト。
    /// </summary>
    string ObjectiveBaseText { get; }

    /// <summary>
    /// 目標表示の進捗値テキスト。
    /// </summary>
    string ObjectiveConditionValue { get; }

    event Func<bool> OnGameClearInteraction;
}
