using UnityEngine;
using TMPro;

public class VersionText : MonoBehaviour
{
    [SerializeField] private TextMeshProUGUI _versionText;
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        _versionText.text = "Ver." + Application.version;
    }
}
