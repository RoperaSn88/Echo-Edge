using System;
using System.Threading;
using UnityEngine;
using Cysharp.Threading.Tasks;
using UnityEngine.AddressableAssets;
using UnityEngine.ResourceManagement.AsyncOperations;

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
        
        private const string WeaponModelPath = "Assets/Addressables/WeaponModels/";
        
        // 武器の最大数は2個
        private const int MaxWeaponNum = 2;
        

        private void Start()
        {
            Instance = this;
        }

        public async UniTask<WeaponActionType> SelectWeapon()
        {
            WeaponActionType result = WeaponActionType.Invalid;
            // 起動時、0番目のPresenterを表示させる
            bool isFirst = true;
            var SelectingPresenter = _presenters[0];

            _cancellationTokenSource = new();
            
            // 武器が選択されるまでループする
            bool isSelected = false;
            WeaponMoveDirection weaponMoveDirection = WeaponMoveDirection.UpToDown;
            
            // ボタンが押された時はbreak スクロールはプレゼンターを切り替えて再度調べる
            while (true)
            {
                // Addressableから武器のモデルを読み込む
                var targetModel = await Addressables.LoadAssetAsync<WeaponModel>(WeaponModelPath + (_selectedWeaponIndex + 1) + ".asset");
                
                // 見つかればプレゼンターにモデルをセットして、操作を待つ
                if (targetModel != null)
                {
                    // ビューにモデルを設定していく
                    SelectingPresenter.SetWeapon(targetModel);
                    
                    SelectingPresenter.AppearUIs(weaponMoveDirection, _cancellationTokenSource.Token).Forget();
                    
                    WeaponActionType actionType = await GetWeaponAction();
                    
                    // 操作の結果で分ける
                    switch (actionType)
                    {
                        case WeaponActionType.Press:
                            Debug.Log("決定");
                            PlayerSwordParameterHolder.SetSwordStatus(targetModel.HP, targetModel.Attack);
                            isSelected = true;
                            result = WeaponActionType.Press;
                            break;
                        case WeaponActionType.Cancel:
                            Debug.Log("キャンセル");
                            isSelected = true;
                            result = WeaponActionType.Cancel;
                            break;
                        case WeaponActionType.SelectUp:
                            Debug.Log("選択上");
                            _selectedWeaponIndex += 1;
                            if(_selectedWeaponIndex >= MaxWeaponNum)
                            {
                                _selectedWeaponIndex = 0;
                            }

                            weaponMoveDirection = WeaponMoveDirection.DownToUp;
                            break;
                        case WeaponActionType.SelectDown:
                            Debug.Log("選択下");
                            _selectedWeaponIndex -= 1;
                            if(_selectedWeaponIndex < 0)
                            {
                                _selectedWeaponIndex = MaxWeaponNum - 1;
                            }
                            weaponMoveDirection = WeaponMoveDirection.UpToDown;
                            break;
                    }

                    if (isSelected)
                    {
                        break;
                    }
                    
                    // キャンセレーションを更新する
                    _cancellationTokenSource.Cancel();
                    _cancellationTokenSource = new();
                    
                    // プレゼンターを切り替える
                    SelectingPresenter.DisappearUIs(weaponMoveDirection, _cancellationTokenSource.Token).Forget();

                    if (isFirst)
                    {
                        SelectingPresenter = _presenters[1];
                        isFirst = false;
                    }
                    else
                    {
                        SelectingPresenter = _presenters[0];
                        isFirst = true;
                    }
                }
                else
                {
                    throw new Exception("武器のモデルが見つかりませんでした");
                }
            }

            SelectingPresenter.DisappearUIs(weaponMoveDirection, _cancellationTokenSource.Token).Forget();

            return result;
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
    }
}
