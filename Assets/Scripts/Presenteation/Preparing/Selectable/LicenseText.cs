using Cysharp.Threading.Tasks;

using EchoEdge.App.Scene;
using EchoEdge.Domain.Scene;

namespace EchoEdge.Presenter.Preparing
{
    public class LicenseText : TMPSelectObject
    {
        private SelectableGroup _group;

        private void Start()
        {
            _group = GetComponentInParent<SelectableGroup>();
        }
        
        public override async UniTask OnDecide()
        {
            await SceneLoader.AdditiveLoadAndWait(GameScene.License);
            await _group.ResetGroup();
        }
    }
}
