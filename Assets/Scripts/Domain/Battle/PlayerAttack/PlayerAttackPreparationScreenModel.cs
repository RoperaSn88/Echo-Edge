using Cysharp.Threading.Tasks;

namespace Domain.Battle.PlayerAttack
{
    /// <summary>
    /// 攻撃準備画面のScreenModel。
    /// 入力の購読自体は行わず、<see cref="PlayerAttackPreparationPhase"/> から呼び出される
    /// メソッド経由で <see cref="PlayerAttackPreparationViewModel"/> を更新する。
    /// </summary>
    public class PlayerAttackPreparationScreenModel
    {
        public PlayerAttackPreparationViewModel PlayerAttackPreparationViewModel { get; }

        public PlayerAttackPreparationScreenModel()
        {
            PlayerAttackPreparationViewModel = new PlayerAttackPreparationViewModel();
        }

        public async UniTask InitializeAsync()
        {
            await PlayerAttackPreparationViewModel.OnInitializeAsync();
        }

        public async UniTask FinalizeAsync()
        {
            await PlayerAttackPreparationViewModel.OnFinalizeAsync();
        }

        /// <summary>
        /// マウスホイールクリックによって、強い一閃にするか
        /// </summary>
        public void ToggleFlashMode()
        {
            PlayerAttackPreparationViewModel.ToggleFlashing();
        }
        
        /// <summary>
        /// マウスホイールでの切り替え入力を受けて、攻撃の種類を切り替える
        /// </summary>
        public void ToggleAttackMode()
        {
            PlayerAttackPreparationViewModel.ToggleAttackMode();
        }
    }
}
