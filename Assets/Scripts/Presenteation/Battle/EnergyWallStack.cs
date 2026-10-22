using System.Collections.Generic;
using UnityEngine;

namespace EchoEdge.Presenter.Battle
{
    /// <summary>
    /// 斜面のエナジーウォール（<see cref="SlopeWallView"/> のプレハブ）のオブジェクトプール。
    /// ビルダーの壁（<see cref="WallStack"/>）とは別に管理する。
    /// </summary>
    public class EnergyWallStack : MonoBehaviour
    {
        [SerializeField]
        private SlopeWallView slopeWallPrefab;

        [SerializeField]
        private Transform wallsParent;

        [SerializeField, Min(0)]
        private int initialPoolSize = 4;

        private readonly Stack<SlopeWallView> _availableWalls = new();

        private void Awake()
        {
            for (int i = 0; i < initialPoolSize; i++)
            {
                var wall = CreateWall();
                if (wall != null)
                {
                    _availableWalls.Push(wall);
                }
            }
        }

        public SlopeWallView GetWall()
        {
            while (_availableWalls.Count > 0)
            {
                var pooledWall = _availableWalls.Pop();
                if (pooledWall != null)
                {
                    pooledWall.gameObject.SetActive(true);
                    return pooledWall;
                }
            }

            var wall = CreateWall();
            if (wall != null)
            {
                wall.gameObject.SetActive(true);
            }
            return wall;
        }

        public void ReturnWall(SlopeWallView wall)
        {
            if (wall == null) return;
            wall.gameObject.SetActive(false);
            _availableWalls.Push(wall);
        }

        private SlopeWallView CreateWall()
        {
            if (slopeWallPrefab == null)
            {
                Debug.LogError("slopeWallPrefab が未設定です。");
                return null;
            }

            var wall = Instantiate(slopeWallPrefab, wallsParent);
            wall.gameObject.SetActive(false);
            return wall;
        }
    }
}
