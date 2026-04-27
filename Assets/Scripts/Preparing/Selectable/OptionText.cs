using Cysharp.Threading.Tasks;

namespace UnityEngine.Selectable
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