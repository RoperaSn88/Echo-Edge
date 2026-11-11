using UnityEngine;
using UnityEngine.InputSystem;

using EchoEdge.Presenter.Player;

namespace EchoEdge.Presenter.Battle
{
    /// <summary>
    /// 敵の攻撃中にプレイヤーが行う防御（プレイヤーフェーズの「防御」行動とは別物）。
    /// 敵の攻撃アニメーションが始まってから終わるまでの間、クリックで防御できる。
    /// クリックすると <see cref="GuardWindow"/> 秒だけ防御状態になり、その後 <see cref="Cooldown"/> 秒は再入力できない。
    /// 敵の3度目の点滅（攻撃の瞬間）に防御状態であれば、その攻撃を完全に防御する。
    /// </summary>
    public static class EnemyAttackGuard
    {
        /// <summary>
        /// 1回のクリックで防御状態になる時間（秒）
        /// </summary>
        public const float GuardWindow = 0.15f;

        /// <summary>
        /// 防御状態が終わってから、次の防御を入力できるようになるまでの時間（秒）
        /// </summary>
        public const float Cooldown = 0.3f;

        private static MouseClick _input;

        /// <summary>
        /// 防御を受け付けている攻撃の数。複数の攻撃が重なっても、全て終わるまで受け付ける
        /// </summary>
        private static int _acceptCount;

        /// <summary>
        /// 最後に防御を入力した時刻（unscaled）
        /// </summary>
        private static float _lastGuardTime = float.NegativeInfinity;

        /// <summary>
        /// 攻撃の瞬間に判定した結果。未判定なら null
        /// </summary>
        private static bool? _judgedResult;

        /// <summary>
        /// 防御を受け付けているか
        /// </summary>
        public static bool IsAccepting => _acceptCount > 0;

        /// <summary>
        /// 今、防御状態（受付時間内）か
        /// </summary>
        public static bool IsGuarding => IsAccepting && Time.unscaledTime - _lastGuardTime <= GuardWindow;

        /// <summary>
        /// 防御の受付を始める。敵の攻撃アニメーションを始めるときに呼ぶ。
        /// 呼んだ回数だけ <see cref="EndAccept"/> を呼ぶこと。
        /// </summary>
        public static void BeginAccept()
        {
            if (_acceptCount == 0)
            {
                _lastGuardTime = float.NegativeInfinity;
                _judgedResult = null;

                if (_input == null)
                {
                    _input = new MouseClick();
                    _input.Mouse.MouseClick.started += OnClick;
                }
                _input.Enable();
            }

            _acceptCount++;
        }

        /// <summary>
        /// 防御の受付を終える。敵の攻撃アニメーション（とダメージ処理）が終わったときに呼ぶ。
        /// </summary>
        public static void EndAccept()
        {
            if (_acceptCount == 0) return;

            _acceptCount--;
            if (_acceptCount == 0)
            {
                _input?.Disable();
                _judgedResult = null;
            }
        }

        /// <summary>
        /// 攻撃の瞬間（敵の3度目の点滅）に、防御状態かを判定して覚えておく。
        /// 実際のダメージ計算はこのあと <see cref="ConsumeResult"/> で結果を受け取る。
        /// </summary>
        public static void Judge()
        {
            _judgedResult = IsGuarding;
        }

        /// <summary>
        /// 攻撃を完全に防御したかを返し、判定結果を消す。
        /// 点滅イベントが無い攻撃（判定前にダメージ計算に来た場合）は、その時点で防御状態かを返す。
        /// </summary>
        public static bool ConsumeResult()
        {
            var result = _judgedResult ?? IsGuarding;
            _judgedResult = null;
            return result;
        }

        private static void OnClick(InputAction.CallbackContext context)
        {
            if (!IsAccepting) return;

            // ポーズ中などは受け付けない
            if (Time.timeScale <= 0f) return;

            var now = Time.unscaledTime;
            if (now < _lastGuardTime + GuardWindow + Cooldown) return;

            _lastGuardTime = now;

            if (PlayerView.Instance != null)
            {
                PlayerView.Instance.PlayGuardFlash(GuardWindow);
            }
        }
    }
}
