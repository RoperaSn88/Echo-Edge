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
            Application.Quit();
            return UniTask.CompletedTask;
        }
    }
}
