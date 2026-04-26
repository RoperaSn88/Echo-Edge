using Cysharp.Threading.Tasks;

namespace UnityEngine.Selectable
{
    /// <summary>
    /// 選択時、オプションシーンを追加ロードするテキストを管理するクラス
    /// </summary>
    public class SettingText : TMPSelectObject
    {
        public override async UniTask OnDecide()
        {
            await SceneLoader.AdditiveLoadAndWait(GameScene.Option);
        }
    }
}
