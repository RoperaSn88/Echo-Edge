using System;
using UnityEngine;
using System.Collections.Generic;
using Cysharp.Threading.Tasks;

using EchoEdge.Domain.Battle;

namespace EchoEdge.Presenter.Battle
{
    /// <summary>
    /// ユニットをインスタンス化して配置するスポナー
    /// </summary>
    public class UnitSpawner : MonoBehaviour
    {
        public static UnitSpawner Instance;

        [SerializeField]
        private GameObject unitPrefab;

        [SerializeField]
        private GameObject flyingUnitPrefab;

        [SerializeField]
        private Transform unitsParent;

        /// <summary>
        /// 『シーカー』を立たせる位置（盤面の外、ステージの奥地）。未設定の場合は CSV の座標のマスに表示する。
        /// </summary>
        [SerializeField]
        private Transform seekerStandPoint;

        [SerializeField, Min(0)]
        private int initialPoolSize = 8;

        private readonly Stack<BaseUnitView> _unitViewPool = new();
        private readonly Stack<FlyingBaseUnitView> _flyingUnitViewPool = new();

        void Awake()
        {
            Instance = this;
            SetupPool();
        }

        /// <summary>
        /// BaseUnit の位置に対応する View を生成し、unit に紐づける。
        /// 飛行ユニット（Skya）の場合は FlyingBaseUnitView を使用する。
        /// </summary>
        /// <param name="unit">配置済みの BaseUnit</param>
        public void SpawnView(BaseUnit unit, EnemyKinds enemyId)
        {
            BaseUnitView view = IsFlyingUnit(enemyId)
                ? (BaseUnitView)GetPooledFlyingView()
                : GetPooledBaseView();

            if (view == null)
            {
                return;
            }

            if (enemyId.IsOffBoard())
            {
                unit.SetView(view);
                SetupOffBoardViewAsync(view, unit, enemyId, seekerStandPoint).Forget();
                return;
            }

            view.Setup(unit.Height, unit.Width, enemyId);
            unit.SetView(view);
        }

        /// <summary>
        /// 盤面の外に立つユニット（『シーカー』）の View を、指定した Transform の位置に立たせる。
        /// 一閃が当たらないよう当たり判定も切る。
        /// </summary>
        private async UniTaskVoid SetupOffBoardViewAsync(BaseUnitView view, BaseUnit unit, EnemyKinds enemyId, Transform standPoint)
        {
            try
            {
                await view.Setup(unit.Height, unit.Width, enemyId);
            }
            catch (Exception e)
            {
                Debug.LogWarning($"UnitSpawner: {enemyId} の View 初期化中に例外が発生しました: {e.Message}");
            }

            if (view == null) return;

            if (standPoint != null)
            {
                view.transform.position = standPoint.position;
            }
            else
            {
                Debug.LogWarning($"UnitSpawner: {enemyId} の立ち位置が設定されていないため、CSV の座標に表示します。");
            }

            view.SetDamageable(false);
        }

        /// <summary>
        /// マップ上で稼働中の全ユニット View の sprite を、真上視点用に寝かせて下げる／元へ戻す。
        /// 一閃準備フェーズの開始・終了時に呼ぶ。
        /// </summary>
        /// <param name="enable">true で寝かせて下げる、false で元へ戻す</param>
        public void SetUnitsTopDownSpritePose(bool enable)
        {
            if (unitsParent == null) return;

            foreach (Transform child in unitsParent)
            {
                if (!child.gameObject.activeSelf) continue;
                if (child.TryGetComponent<BaseUnitView>(out var view))
                {
                    view.SetTopDownSpritePose(enable);
                }
            }
        }

        public void ReturnView(BaseUnitView view)
        {
            if (view == null) return;
            view.gameObject.SetActive(false);
            if (view is FlyingBaseUnitView flyingView)
            {
                _flyingUnitViewPool.Push(flyingView);
            }
            else
            {
                _unitViewPool.Push(view);
            }
        }

        private static bool IsFlyingUnit(EnemyKinds enemyId) => enemyId == EnemyKinds.Skya;

        private void SetupPool()
        {
            for (int i = 0; i < initialPoolSize; i++)
            {
                var view = CreatePooledView();
                if (view != null)
                {
                    _unitViewPool.Push(view);
                }
            }
        }

        private BaseUnitView GetPooledBaseView()
        {
            while (_unitViewPool.Count > 0)
            {
                var pooledView = _unitViewPool.Pop();
                if (pooledView != null)
                {
                    pooledView.gameObject.SetActive(true);
                    return pooledView;
                }
            }

            var view = CreatePooledView();
            if (view != null)
            {
                view.gameObject.SetActive(true);
            }
            return view;
        }

        private FlyingBaseUnitView GetPooledFlyingView()
        {
            while (_flyingUnitViewPool.Count > 0)
            {
                var pooledView = _flyingUnitViewPool.Pop();
                if (pooledView != null)
                {
                    pooledView.gameObject.SetActive(true);
                    return pooledView;
                }
            }

            var view = CreateFlyingPooledView();
            if (view != null)
            {
                view.gameObject.SetActive(true);
            }
            return view;
        }

        private BaseUnitView CreatePooledView()
        {
            if (unitPrefab == null || !unitPrefab.TryGetComponent<BaseUnitView>(out _))
            {
                Debug.LogError("unitPrefab に BaseUnitView がアタッチされていません。");
                return null;
            }

            var obj = Instantiate(unitPrefab, unitsParent);
            if (!obj.TryGetComponent<BaseUnitView>(out var view))
            {
                Debug.LogError("unitPrefab に BaseUnitView がアタッチされていません。");
                return null;
            }

            obj.SetActive(false);
            return view;
        }

        private FlyingBaseUnitView CreateFlyingPooledView()
        {
            if (flyingUnitPrefab == null || !flyingUnitPrefab.TryGetComponent<FlyingBaseUnitView>(out _))
            {
                Debug.LogError("flyingUnitPrefab に FlyingBaseUnitView がアタッチされていません。");
                return null;
            }

            var obj = Instantiate(flyingUnitPrefab, unitsParent);
            if (!obj.TryGetComponent<FlyingBaseUnitView>(out var view))
            {
                Debug.LogError("flyingUnitPrefab に FlyingBaseUnitView がアタッチされていません。");
                return null;
            }

            obj.SetActive(false);
            return view;
        }
    }
}
