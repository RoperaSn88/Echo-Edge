using Cysharp.Threading.Tasks;

namespace UnityEngine.Selectable
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
            AudioManager.Instance.PlaySe(SeAudioType.Click);
            await SceneLoader.AdditiveLoadAndWait(GameScene.License);
            await _group.ResetGroup();
        }
    }
}