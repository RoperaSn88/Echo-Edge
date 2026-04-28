using Cysharp.Threading.Tasks;

namespace UnityEngine.Selectable
{
    /// <summary>
    /// 選択時、ゲームを終了するテキストを管理するクラス
    /// </summary>
    public class QuitText : TMPSelectObject
    {
        public override UniTask OnDecide()
        {
#if UNITY_EDITOR
            UnityEditor.EditorApplication.isPlaying = false;
#else
            Application.Quit();
#endif
            return UniTask.CompletedTask;
        }
    }
}
