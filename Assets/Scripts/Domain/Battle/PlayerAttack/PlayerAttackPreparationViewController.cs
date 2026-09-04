using System.Threading;
using Cysharp.Threading.Tasks;
using UnityEngine;

using EchoEdge.Presenter.Player;
using EchoEdge.Utils;

namespace EchoEdge.Domain.Battle
{
    /// <summary>
    /// ビューにモデルの状態を反映させるコントローラークラス。
    /// <see cref="PlayerAttackPreparationViewModel"/> の状態変化を購読し、変化した場合に
    /// <see cref="PlayerAttackPreparationView"/> に通知する。表示内容の判断はこのクラスが担い、
    /// View は渡された値を表示するだけにする。
    /// </summary>
    public class PlayerAttackPreparationViewController: MonoBehaviour
    {
        [SerializeField]
        private PlayerAttackPreparationView _view;

        private PlayerAttackPreparationViewModel _viewModel;

        private CancellationTokenSource _cancellationTokenSource;

        /// <summary>
        /// 監視対象の<see cref="PlayerAttackPreparationViewModel"/>を登録し、状態変化の購読を開始する。
        /// ScreenModelが保持するインスタンスをそのまま受け取ることで、状態の保持場所を一箇所に保つ。
        /// </summary>
        public void Initialize(PlayerAttackPreparationViewModel viewModel)
        {
            if (_viewModel != null)
            {
                _viewModel.IsFlashingChanged -= OnIsFlashingChanged;
                _viewModel.AttackKindChanged -= OnAttackKindChanged;
            }
            _cancellationTokenSource = new CancellationTokenSource();
            _viewModel = viewModel;
            
            _viewModel.IsFlashingChanged += OnIsFlashingChanged;
            _viewModel.IsFlashingChanged += PlayerController.Instance.SetFlashAttack;
            
            _viewModel.AttackKindChanged += OnAttackKindChanged;
            _viewModel.AttackKindChanged += PlayerController.Instance.SetAttackKind;

            // 同じバトル内で維持されている攻撃種類を PlayerController へ反映する(表示は ShowAsync 側で行う)。
            PlayerController.Instance.SetAttackKind(_viewModel.AttackKind);
            // Flash(一閃)フラグは毎ターン初期化する。OnInitializeAsync で false に戻した状態を PlayerController へ反映する。
            PlayerController.Instance.SetFlashAttack(_viewModel.IsFlashing);
        }

        public async UniTask InitializeAsync(CancellationToken token)
        {
            await _view.InitializeAsync(token);
        }

        public async UniTask ShowAsync(CancellationToken token)
        {
            await _view.ShowAttackPreparationAsync(BuildAttackName(), BuildDescriptionName(), BuildAttackColor(), token);
        }

        public async UniTask HideAsync(CancellationToken token)
        {
            await _view.HideAttackPreparationAsync(token);
        }

        private void OnIsFlashingChanged(bool isFlashing)
        {
            _cancellationTokenSource?.Cancel();
            _cancellationTokenSource?.Dispose();
            _cancellationTokenSource = new CancellationTokenSource();
            UpdateAttackViewAsync(_cancellationTokenSource.Token).Forget();
        }

        private void OnAttackKindChanged(PlayerAttackKinds attackKind)
        {
            _cancellationTokenSource?.Cancel();
            _cancellationTokenSource?.Dispose();
            _cancellationTokenSource = new CancellationTokenSource();
            UpdateAttackViewAsync(_cancellationTokenSource.Token).Forget();
        }

        private async UniTask UpdateAttackViewAsync(CancellationToken token)
        {
            await _view.UpdateAttackViewAsync(BuildAttackName(), BuildDescriptionName(), BuildAttackColor(), token);
        }

        private string BuildAttackName()
        {
            switch (_viewModel.AttackKind)
            {
                case PlayerAttackKinds.Reflect:
                    return "反射";
                case PlayerAttackKinds.Pierce:
                    return "貫通";
                case PlayerAttackKinds.Bomb:
                    return "爆発";
                case PlayerAttackKinds.Curve:
                    return "弧曲";
                default:
                    return "不明";
            }
        }
        
        private string BuildDescriptionName()
        {
            switch (_viewModel.AttackKind)
            {
                case PlayerAttackKinds.Reflect:
                    return "反射攻撃の説明";
                case PlayerAttackKinds.Pierce:
                    return "貫通攻撃の説明";
                case PlayerAttackKinds.Bomb:
                    return "爆発攻撃の説明";
                case PlayerAttackKinds.Curve:
                    return "弧曲攻撃の説明";
                default:
                    return "不明な攻撃の説明";
            }
        }
        
        private Color BuildAttackColor()
        {
            switch (_viewModel.AttackKind)
            {
                case PlayerAttackKinds.Reflect:
                    return EchoEdgeConstants.BlueColor();
                case PlayerAttackKinds.Pierce:
                    return Color.red;
                case PlayerAttackKinds.Bomb:
                    return Color.yellow;
                case PlayerAttackKinds.Curve:
                    return Color.green;
                default:
                    return Color.white;
            }
        }

        private void OnDestroy()
        {
            if (_viewModel != null)
            {
                _viewModel.IsFlashingChanged -= OnIsFlashingChanged;
                _viewModel.AttackKindChanged -= OnAttackKindChanged;
            }
        }
    }
}
