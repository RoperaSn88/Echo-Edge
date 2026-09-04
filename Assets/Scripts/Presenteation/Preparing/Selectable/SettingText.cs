using Cysharp.Threading.Tasks;

using EchoEdge.App.Scene;

namespace EchoEdge.Presenter.Preparing
{
    /// <summary>
    /// 選択時、オプションシーンを追加ロードするテキストを管理するクラス
    /// </summary>
    public class SettingText : TMPSelectObject
    {
        private const int OptionSceneBuildIndex = 3;

        public override async UniTask OnDecide()
        {
            await SceneLoader.AdditiveLoadAndWait(OptionSceneBuildIndex);
        }
    }
}
