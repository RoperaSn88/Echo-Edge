using Cysharp.Threading.Tasks;

namespace UnityEngine.Selectable
{
    public class CreditText : TMPSelectObject
    {
        private SelectableGroup _group;

        private void Start()
        {
            _group = GetComponentInParent<SelectableGroup>();
        }
        
        public override async UniTask OnDecide()
        {
            await SceneLoader.AdditiveLoadAndWait(GameScene.Credit);
            await _group.ResetGroup();
        }
    }
}