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
        
        /// <summary>
        /// 現在選択されている武器のインデックス
        /// </summary>
        public int SelectedWeaponIndex => _selectedWeaponIndex;
        
        [SerializeField, Tooltip("プレゼンターを突っ込む")]
        private WeaponPresenter[] _presenters= new WeaponPresenter[2];
        
        private static WeaponModel _targetModel;
        public static WeaponModel TargetModel => _targetModel;

        private CancellationTokenSource _cancellationTokenSource;
        
        private const string WeaponModelPath = "Assets/Addressables/WeaponModels/";
        
        // 武器の最大数は2個
        private const int MaxWeaponNum = 2;
        

        private void Start()
        {
            Instance = this;
        }

        /// <summary>
        /// 全ての武器UIを即時非表示にする
        /// </summary>
        public void HideAllWeaponUIs()
        {
            foreach (var presenter in _presenters)
            {
                presenter.HideUIs();
            }
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
            
            PlayerView.Instance.Animator.SetBool($"WeaponF", true);
            
            while (true)
            {
                // Addressableから武器のモデルを読み込む
                _targetModel = await Addressables.LoadAssetAsync<WeaponModel>(WeaponModelPath + (_selectedWeaponIndex + 1) + ".asset");
                
                // 見つかればプレゼンターにモデルをセットして、操作を待つ
                if (_targetModel != null)
                {
                    // プレイヤーのアニメーション設定
                    PlayerView.Instance.Animator.SetInteger($"WeaponInteger", _selectedWeaponIndex + 1);
                    
                    // ビューにモデルを設定していく
                    SelectingPresenter.SetWeapon(_targetModel);
                    
                    SelectingPresenter.AppearUIs(weaponMoveDirection, _cancellationTokenSource.Token).Forget();
                    
                    WeaponActionType actionType = await GetWeaponAction();
                    
                    // 操作の結果で分ける
                    switch (actionType)
                    {
                        case WeaponActionType.Press:

                            if (_targetModel.WeaponCost > EnergyManager.CurrentEnergy)
                            {
                                // エネルギーが足りない場合は何もしない
                                break;
                            }
                            else {
                                isSelected = true;
                                result = WeaponActionType.Press;
                            }
                            break;
                        case WeaponActionType.Cancel:
                            isSelected = true;
                            result = WeaponActionType.Cancel;
                            break;
                        case WeaponActionType.SelectUp:
                            _selectedWeaponIndex += 1;
                            if(_selectedWeaponIndex >= MaxWeaponNum)
                            {
                                _selectedWeaponIndex = 0;
                            }

                            weaponMoveDirection = WeaponMoveDirection.DownToUp;
                            break;
                        case WeaponActionType.SelectDown:
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
            PlayerView.Instance.Animator.SetBool($"WeaponF", false);

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
