namespace EchoEdge.Domain.Battle
{
    /// <summary>
    /// ステージ CSV の行動リストで「スキル」を指定されたときに、
    /// スキルを使えるかの判定や事前準備が必要な <see cref="IUnitAction"/> が実装する。
    /// 実装していない行動は、指定されればいつでもスキル（<see cref="IUnitAction.Specific"/>）を行う。
    /// </summary>
    public interface IScriptedSkillAction
    {
        /// <summary>
        /// スキルを使う準備をする（通常の <see cref="IUnitAction.Act"/> がスキルを選んだときと同じ準備）。
        /// </summary>
        /// <returns>スキルを使えるなら true。false のときはこのフェーズは何もしない</returns>
        bool TryPrepareSkill(int selfHeight, int selfWidth);
    }
}
