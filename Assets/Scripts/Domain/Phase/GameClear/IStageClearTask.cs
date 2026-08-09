/// <summary>
/// ステージクリア条件を1つ表すタスクのインターフェース。
/// 進捗の保持・更新・満了判定・発火タイミングの購読までを実装側が自己完結で行う。
/// GameClearManager はこのインターフェース越しにアクティブな1つのタスクを扱うだけで、
/// 条件の中身（何をトリガーに、どう進捗を計算するか）には関知しない。
/// </summary>
public interface IStageClearTask
{
    /// <summary>
    /// 目標表示のベーステキスト。
    /// </summary>
    string ObjectiveBaseText { get; }

    /// <summary>
    /// 目標表示の進捗値テキスト。
    /// </summary>
    string ObjectiveConditionValue { get; }

    /// <summary>
    /// クリア条件を満たしているか。
    /// </summary>
    bool IsGameClearCondition { get; }

    /// <summary>
    /// ステージ（ウェーブ）開始時に条件の初期値を設定する。
    /// </summary>
    void Initialize(int conditionValue);

    /// <summary>
    /// この条件の進捗更新に必要なイベント等の購読を開始する。
    /// GameClearManager がこのタスクをアクティブにする際に呼び出す。
    /// </summary>
    void Subscribe();

    /// <summary>
    /// 購読を解除する。GameClearManager がこのタスクを非アクティブにする際に呼び出す。
    /// </summary>
    void Unsubscribe();
}
