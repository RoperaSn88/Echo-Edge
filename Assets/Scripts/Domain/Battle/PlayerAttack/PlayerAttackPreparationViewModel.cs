using System;
using Cysharp.Threading.Tasks;

namespace EchoEdge.Domain.Battle
{
    /// <summary>
    /// 攻撃準備フェーズに関する状態を保持する唯一のデータソース。
    /// 入力の購読は行わず、状態の変更はSetXxx系メソッド経由でのみ行い、変化時にイベントを発火する。
    /// ViewControllerはこのイベントを購読して表示に反映する。
    /// </summary>
    public class PlayerAttackPreparationViewModel
    {
        /// <summary>
        /// <see cref="CycleAttackMode"/> で切り替える対象の攻撃種類。<see cref="PlayerAttackKinds.Invalid"/> は含めない。
        /// </summary>
        private static readonly PlayerAttackKinds[] CyclableAttackKinds =
        {
            PlayerAttackKinds.Reflect,
            PlayerAttackKinds.Pierce,
            PlayerAttackKinds.Curve,
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
            // AttackKind はリセットしない。同じバトル内(このインスタンスの生存中)は
            // 前回のフェーズで選んだ攻撃を維持する。バトルが変わればインスタンスごと作り直され、
            // 既定値の反射(PlayerAttackKinds.Reflect)に戻る。
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
        /// <see cref="AttackKind"/> を <see cref="CyclableAttackKinds"/> の並び順で切り替える。
        /// スクロール上方向は <paramref name="forward"/> = true、下方向は false を渡す。
        /// 端に達したら反対側へ循環する。
        /// </summary>
        /// <param name="forward">true で次の種類へ、false で前の種類へ。</param>
        public void CycleAttackMode(bool forward)
        {
            int count = CyclableAttackKinds.Length;
            int currentIndex = Array.IndexOf(CyclableAttackKinds, AttackKind);
            if (currentIndex < 0)
            {
                SetAttackKind(CyclableAttackKinds[0]);
                return;
            }

            int step = forward ? 1 : -1;
            int nextIndex = ((currentIndex + step) % count + count) % count;
            SetAttackKind(CyclableAttackKinds[nextIndex]);
        }
    }
}
