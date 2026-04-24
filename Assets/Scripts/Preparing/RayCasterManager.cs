using System;
using System.Collections.Generic;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.UI;
using Pointer = UnityEngine.InputSystem.Pointer;

namespace UnityEngine
{
    public class RayCasterManager: MonoBehaviour
    {
        public static RayCasterManager Instance;
        
        [SerializeField]
        private Transform _origin;
        
        [SerializeField]
        private EventSystem _eventSystem;
        
        [SerializeField]
        private GraphicRaycaster _graphicRaycaster;
        
        private List<RaycastResult> _raycastResults = new List<RaycastResult>();
        
        private PreparingAction _preparingAction;
        private void Start()
        {
            Instance = this;
            _preparingAction = new PreparingAction();
            _preparingAction.Preparing.Click.performed += OnClick;
            _preparingAction.Enable();
        }

        private void Update()
        {
            PointerEventData pointerData = new PointerEventData(_eventSystem);
            pointerData.position = Pointer.current.position.ReadValue();

            _raycastResults = new List<RaycastResult>();
            _graphicRaycaster.Raycast(pointerData, _raycastResults);
        }
        
        /// <summary>
        /// 選択されたUIを返却する
        /// </summary>
        /// <returns></returns>
        public ISelectInterface GetClickedObject()
        {
            if (_raycastResults[0].gameObject.TryGetComponent(out ISelectInterface selectInterface))
            {
                return selectInterface;
            }
            return null;
        }

        private void OnClick(InputAction.CallbackContext context)
        {
            
        }

        private void OnDestroy()
        {
            _preparingAction.Preparing.Click.performed -= OnClick;
            _preparingAction.Disable();
        }
    }
}