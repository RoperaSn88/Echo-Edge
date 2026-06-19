using UnityEngine;
using UnityEngine.InputSystem;

public class PointerOverlayController : MonoBehaviour
{
    [SerializeField] private RectTransform _pointerRectTransform;

    private void Awake()
    {
        Cursor.visible = false;
    }

    private void Update()
    {
        if (_pointerRectTransform == null || Pointer.current == null)
        {
            return;
        }

        _pointerRectTransform.position = Pointer.current.position.ReadValue();
    }

    private void OnDestroy()
    {
        Cursor.visible = true;
    }
}
