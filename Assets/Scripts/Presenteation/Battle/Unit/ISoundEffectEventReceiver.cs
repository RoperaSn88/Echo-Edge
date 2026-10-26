using EchoEdge.Infra.Audio;

namespace EchoEdge.Presenter.Battle
{
    /// <summary>
    /// アニメーションイベントから SE を再生するためのインターフェース。
    /// Animator と同じ GameObject のコンポーネントに実装すると、アニメーションイベントの Function に
    /// SoundEffectPlay を指定し、引数のドロップダウンで SE を選んで鳴らせる。
    /// </summary>
    public interface ISoundEffectEventReceiver
    {
        /// <summary>
        /// 指定した SE を再生する（アニメーションイベント用）
        /// </summary>
        /// <param name="seType">再生する SE の種類</param>
        void SoundEffectPlay(SeAudioType seType);
    }
}
