using UnityEngine;

public class BuildingManager : MonoBehaviour
{
    public static BuildingManager Instance;

    [SerializeField]
    private WallObjectPool wallObjectPool;

    void Start()
    {
        Instance = this;
    }

    public void SetBuilding(int h, int w)
    {
        if (wallObjectPool == null)
        {
            wallObjectPool = FindObjectOfType<WallObjectPool>();
        }

        if (wallObjectPool == null)
        {
            Debug.LogError("WallObjectPool が見つかりません。");
            return;
        }

        var v = wallObjectPool.GetWall();
        if (v == null)
        {
            return;
        }
        v.Set(h, w);
    }
}
