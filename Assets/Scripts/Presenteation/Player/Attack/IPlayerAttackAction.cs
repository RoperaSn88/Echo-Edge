using Cysharp.Threading.Tasks;
using UnityEngine;

namespace EchoEdge.Presenter.Player
{
    /// <summary>
    /// プレイヤーの攻撃1種類分の処理を表すインターフェース。
    /// 攻撃の種類(反射・貫通・爆発 × 通常・めちゃくちゃ早い一閃)ごとにこのインターフェースを実装したクラスを用意し、
    /// PlayerControllerはインターフェース越しに呼び出すことで、攻撃固有の移動処理とダメージ判定処理を切り替える。
    /// </summary>
    public interface IPlayerAttackAction
    {
        /// <summary>
        /// 攻撃を実行する。
        /// </summary>
        /// <param name="targetPos">ポインターの先の位置</param>
        UniTask ExecuteAsync(Vector3 targetPos);

        /// <summary>
        /// この攻撃を実行中にOnTriggerEnterが発生した際のダメージ判定処理。
        /// トリガーによるダメージ判定を必要としない攻撃は何もしない実装にしてよい。
        /// </summary>
        /// <param name="other">相手の当たり判定</param>
        void OnTriggerEnter(Collider other);

        /// <summary>
        /// 壁などに当たったときの判定処理。
        /// </summary>
        /// <param name="collision"></param>
        void OnCollisionEnter(Collision collision);
    }
}
