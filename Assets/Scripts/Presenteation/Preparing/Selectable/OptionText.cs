using Cysharp.Threading.Tasks;

using EchoEdge.App.Scene;
using EchoEdge.Domain.Scene;

namespace EchoEdge.Presenter.Preparing
{
    public class OptionText : TMPSelectObject
    {
        private SelectableGroup _group;

        private void Start()
        {
            _group = GetComponentInParent<SelectableGroup>();
        }
        
        public override async UniTask OnDecide()
        {
            await SceneLoader.AdditiveLoadAndWait(GameScene.Option);
            await _group.ResetGroup();
        }
    }
}
