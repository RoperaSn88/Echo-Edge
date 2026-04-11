using System;
using System.Threading;
using UnityEngine;
using Cysharp.Threading.Tasks;

namespace UI.Weapon
{
    public class WeaponController: MonoBehaviour
    {
        public static WeaponController Instance;
        
        [SerializeField, Tooltip("選択されている武器のインデックス")]
        private int _selectedWeaponIndex = 0;
        
        [SerializeField, Tooltip("プレゼンターを突っ込む")]
        private WeaponPresenter[] _presenters= new WeaponPresenter[2];

        private CancellationTokenSource _cancellationTokenSource;
        

        private void Start()
        {
            Instance = this;
        }

        public async UniTask SelectWeapon()
        {
            // 起動時、0番目のPresenterを表示させる
            bool isFirst = true;
            var SelectingPresenter = _presenters[0];

            _cancellationTokenSource = new();
            
            await SelectingPresenter.AppearUIs(_cancellationTokenSource.Token);
            
            while (true)
            {
                WeaponActionType actionType = await GetWeaponAction();
                switch (actionType)
                {
                    case WeaponActionType.Press:
                        Debug.Log("決定");
                        break;
                    case WeaponActionType.Cancel:
                        Debug.Log("キャンセル");
                        break;
                    case WeaponActionType.SelectUp:
                        Debug.Log("選択上");
                        break;
                    case WeaponActionType.SelectDown:
                        Debug.Log("選択下");
                        break;
                }
                
                _cancellationTokenSource.Cancel();
                _cancellationTokenSource = new();
            }
        }
        
        async UniTask<WeaponActionType> GetWeaponAction()
        {
            WeaponAction weaponAction = new WeaponAction();
            weaponAction.Enable();
            WeaponActionType result;
            
            while (true)
            {
                if (weaponAction.Mouse.MouseClick.IsPressed())
                {
                    result = WeaponActionType.Press;
                    break;
                }
                else if (weaponAction.Mouse.WeaponCancel.IsPressed())
                {
                    result = WeaponActionType.Cancel;
                    break;
                }
                else if (weaponAction.Mouse.Scroll.ReadValue<float>() == 1)
                {
                    result = WeaponActionType.SelectUp;
                    break;
                }
                else if (weaponAction.Mouse.Scroll.ReadValue<float>() == -1)
                {
                    result = WeaponActionType.SelectDown;
                    break;
                }

                await UniTask.Yield();
            }
            
            weaponAction.Dispose();
            return result;
        }


        enum WeaponActionType
        {
            Press,
            Cancel,
            SelectUp,
            SelectDown
        }
    }
}