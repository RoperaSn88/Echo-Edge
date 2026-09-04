using UnityEngine;

namespace EchoEdge.Presenter.Scene
{
    [System.Serializable, CreateAssetMenu(menuName = "License")]
    public class LicenseComposer:ScriptableObject
    {
        [SerializeField]
        private string licenseName;

        public string LicenseName => licenseName;

        [SerializeField,TextArea]
        private string licenseIntroduction;
        public string LicenseIntroduction => licenseIntroduction;
    }
}
