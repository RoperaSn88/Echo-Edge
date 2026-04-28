using Cysharp.Threading.Tasks;

namespace UnityEngine.Selectable
{
    /// <summary>
    /// 選択時、ステージをロードするテキストを管理するクラス
    /// </summary>
    public class StartText : TMPSelectObject
    {
        public override UniTask OnDecide()
        {
            SceneLoader.Load(GameScene.MainGame);
            return UniTask.CompletedTask;
        }
    }
}
