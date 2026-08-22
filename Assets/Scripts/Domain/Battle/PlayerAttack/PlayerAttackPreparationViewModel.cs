using System;
using Cysharp.Threading.Tasks;

namespace Domain.Battle.PlayerAttack
{
    /// <summary>
    /// 攻撃準備フェーズに関する状態を保持する唯一のデータソース。
    /// 入力の購読は行わず、状態の変更はSetXxx系メソッド経由でのみ行い、変化時にイベントを発火する。
    /// ViewControllerはこのイベントを購読して表示に反映する。
    /// </summary>
    public class PlayerAttackPreparationViewModel
    {
        /// <summary>
        /// <see cref="ToggleAttackMode"/> で切り替える対象の攻撃種類。<see cref="PlayerAttackKinds.Invalid"/> は含めない。
        /// </summary>
        private static readonly PlayerAttackKinds[] CyclableAttackKinds =
        {
            PlayerAttackKinds.Reflect,
            PlayerAttackKinds.Pierce,
            PlayerAttackKinds.Bomb,
        };

        public bool IsPrepared { get; set; }

        public bool IsFlashing { get; private set; }

        public PlayerAttackKinds AttackKind { get; private set; }

        /// <summary>
        /// <see cref="IsFlashing"/> が変化した際に発火する。
        /// </summary>
        public event Action<bool> IsFlashingChanged;

        /// <summary>
        /// <see cref="AttackKind"/> が変化した際に発火する。
        /// </summary>
        public event Action<PlayerAttackKinds> AttackKindChanged;

        public async UniTask OnInitializeAsync()
        {
            IsPrepared = false;
            IsFlashing = false;
            AttackKind = PlayerAttackKinds.Reflect;
        }

        public async UniTask OnFinalizeAsync()
        {
            IsPrepared = false;
            IsFlashing = false;
        }

        public void SetAttackKind(PlayerAttackKinds kind)
        {
            if (AttackKind == kind) return;

            AttackKind = kind;
            AttackKindChanged?.Invoke(AttackKind);
        }

        /// <summary>
        /// マウスホイールで切り替える、めちゃくちゃ早い一閃モードかどうかを設定する。
        /// </summary>
        public void SetFlashing(bool isFlashing)
        {
            if (IsFlashing == isFlashing) return;

            IsFlashing = isFlashing;
            IsFlashingChanged?.Invoke(IsFlashing);
        }

        /// <summary>
        /// <see cref="IsFlashing"/> を反転させる。
        /// </summary>
        public void ToggleFlashing()
        {
            SetFlashing(!IsFlashing);
        }

        /// <summary>
        /// <see cref="AttackKind"/> を反射・貫通・爆発の順で次の攻撃種類に切り替える。
        /// </summary>
        public void ToggleAttackMode()
        {
            int currentIndex = Array.IndexOf(CyclableAttackKinds, AttackKind);
            int nextIndex = currentIndex < 0 ? 0 : (currentIndex + 1) % CyclableAttackKinds.Length;
            SetAttackKind(CyclableAttackKinds[nextIndex]);
        }
    }
}
