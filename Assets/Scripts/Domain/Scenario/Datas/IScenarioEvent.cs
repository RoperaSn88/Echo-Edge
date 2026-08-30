using UnityEngine;

namespace Domain.Scenario
{
    /// <summary>
    /// シナリオ上で発生するイベントのマーカーインターフェース。
    /// </summary>
    public interface IScenarioEvent
    {
        /// <summary>
        /// このイベントの実行と同時に変更する背景画像。
        /// 値が設定されていない場合は背景を変更しない。
        /// </summary>
        Sprite Background { get; }
    }
}
