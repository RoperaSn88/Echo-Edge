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
        /// QTE (攻撃/防御) が初めて出現したとき
        /// </summary>
        QTEAppear,

        /// <summary>
        /// 追加反射の分度器QTEが初めて出現したとき
        /// </summary>
        ReflectGauge,
    }
}