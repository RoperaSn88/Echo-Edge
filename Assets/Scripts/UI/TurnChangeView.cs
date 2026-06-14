using Cysharp.Threading.Tasks;
using UnityEngine;
using UnityEngine.UI;

public class TurnChangeView : MonoBehaviour
{
    [SerializeField] private Image _turnChangeImage;
    [SerializeField] private Text _turnChangeText;

    private void Start()
    {
        _turnChangeImage.gameObject.SetActive(false);
        _turnChangeText.gameObject.SetActive(false);
    }

    public async UniTask ShowTurnChange(string text)
    {
        
    }
}