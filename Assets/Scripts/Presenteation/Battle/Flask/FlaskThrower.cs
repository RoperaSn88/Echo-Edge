using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using UnityEngine;

namespace EchoEdge.Presenter.Battle
{
    /// <summary>
    /// 『シーカー』のフラスコ投げをまとめるクラス。シーンに1つ置く。
    /// 1. シーカーの行動が決まったら、投げ先を <see cref="Reserve"/> で予約する
    /// 2. シーカーのスキルアニメーションのイベント（BaseUnitView.ThrowFlask）で、予約したフラスコを投げる
    /// 3. シーカーは <see cref="WaitLandedAsync"/> で全フラスコの着地を待ってから、エナーを出す
    /// </summary>
    public class FlaskThrower : MonoBehaviour
    {
        public static FlaskThrower Instance { get; private set; }

        [SerializeField]
        private FlaskPool _flaskPool;

        [SerializeField, Tooltip("投げる人の位置から見た、フラスコが手から離れる位置")]
        private Vector3 _releaseOffset = new Vector3(0f, 1f, 0f);

        [SerializeField, Tooltip("投げ先のマスの床から見た、フラスコが着地する高さ")]
        private float _landingHeight = 0.25f;

        [SerializeField, Min(0f), Tooltip("複数投げるときの、1本ごとの間隔（秒）")]
        private float _throwInterval = 0.1f;

        private readonly List<Vector3> _reservedTargets = new();
        private readonly List<UniTask> _flyingFlasks = new();

        private void Awake()
        {
            Instance = this;
        }

        private void OnDestroy()
        {
            if (Instance == this) Instance = null;
        }

        /// <summary>
        /// 投げ先のマスを予約する。前の予約は捨てる。
        /// </summary>
        public void Reserve(IEnumerable<Vector3> cellPositions)
        {
            _reservedTargets.Clear();
            foreach (var position in cellPositions)
            {
                _reservedTargets.Add(position + Vector3.up * _landingHeight);
            }
        }

        /// <summary>
        /// 予約したフラスコを、thrower の位置から投げる。予約が無ければ何もしない。
        /// </summary>
        public void ThrowReserved(Transform thrower)
        {
            if (_reservedTargets.Count == 0) return;

            Vector3 from = thrower != null ? thrower.position + _releaseOffset : _reservedTargets[0];
            for (int i = 0; i < _reservedTargets.Count; i++)
            {
                _flyingFlasks.Add(ThrowOneAsync(from, _reservedTargets[i], _throwInterval * i));
            }
            _reservedTargets.Clear();
        }

        /// <summary>
        /// 投げたフラスコがすべて着地するまで待つ。
        /// アニメーションイベントが来ずにまだ投げていない予約があれば、fallbackThrower の位置からここで投げる。
        /// </summary>
        public async UniTask WaitLandedAsync(Transform fallbackThrower)
        {
            if (_reservedTargets.Count > 0)
            {
                ThrowReserved(fallbackThrower);
            }

            var flasks = _flyingFlasks.ToArray();
            _flyingFlasks.Clear();
            await UniTask.WhenAll(flasks);
        }

        private async UniTask ThrowOneAsync(Vector3 from, Vector3 to, float delay)
        {
            if (delay > 0f)
            {
                await UniTask.Delay(System.TimeSpan.FromSeconds(delay), cancellationToken: destroyCancellationToken);
            }

            if (_flaskPool == null)
            {
                Debug.LogWarning($"{nameof(FlaskThrower)}: Flask Pool が未設定のため、フラスコを投げずに進めます。");
                return;
            }

            var flask = await _flaskPool.GetPooledObject() as FlaskObject;
            if (flask == null)
            {
                Debug.LogWarning($"{nameof(FlaskThrower)}: Flask Pool のプレハブに FlaskObject が付いていません。");
                return;
            }

            await flask.ThrowAsync(from, to);
        }
    }
}
