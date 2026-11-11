using Unity.VisualScripting;

namespace EchoEdge.Infra.Tutorial
{
    public enum TutorialKinds
    {
        FirstBattle,
        Reflect,
        Attack,
        Enemy,
        Final,
        Pierce,
        FlashAttack,

        /// <summary>
        /// 攻撃QTE が初めて出現したとき（敵の攻撃時の防御QTEは廃止）
        /// </summary>
        QTEAppear,

        /// <summary>
        /// 追加反射の分度器QTEが初めて出現したとき
        /// </summary>
        ReflectGauge,

        /// <summary>
        /// プレイヤーフェーズの行動選択ボタン (一閃/装備品/防御/回復) が初めて表示されたとき
        /// </summary>
        Player,
    }
}