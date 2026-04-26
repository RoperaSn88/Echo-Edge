using System;
using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using Mono.Cecil.Cil;
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
        PointerEventData pointerData;

        private ISelectable _selecting;
        private void Start()
        {
            Instance = this;
            pointerData = new PointerEventData(_eventSystem);
            _preparingAction = new PreparingAction();
            _preparingAction.Enable();
            _selecting = null;
        }

        /// <summary>
        /// クリックされたインターフェースを返却する
        /// </summary>
        /// <returns></returns>
        public async UniTask<ISelectable> Selecting()
        {
            while (true)
            {
                pointerData.position = Pointer.current.position.ReadValue();

                _raycastResults = new List<RaycastResult>();
                _graphicRaycaster.Raycast(pointerData, _raycastResults);
                var selecting = GetClickedObject();
                if (_selecting != selecting)
                {
                    if(_selecting != null) _selecting.OnDeselect();
                    _selecting = selecting;
                    if(_selecting != null) _selecting.OnSelect();
                }

                await UniTask.Yield();
                if (_preparingAction.Preparing.Click.IsPressed())
                {
                    return _selecting;
                }
            }
        }
        
        /// <summary>
        /// 選択されたUIを返却する
        /// </summary>
        /// <returns></returns>
        public ISelectable GetClickedObject()
        {
            if(_raycastResults.Count == 0) return null;
            if (_raycastResults[0].gameObject.TryGetComponent(out ISelectable selectInterface))
            {
                return selectInterface;
            }
            return null;
        }

        private void OnDestroy()
        {
            _preparingAction.Disable();
        }
    }
}