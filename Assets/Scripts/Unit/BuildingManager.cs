using UnityEngine;

public class BuildingManager : MonoBehaviour
{
    public static BuildingManager Instance;

    [SerializeField]
    private WallStack wallStack;

    void Start()
    {
        Instance = this;
        if (wallStack == null)
        {
            var wallStacks = FindObjectsByType<WallStack>(FindObjectsSortMode.None);
            if (wallStacks.Length > 0)
            {
                wallStack = wallStacks[0];
            }
        }
    }

    public void SetBuilding(int h, int w)
    {
        if (wallStack == null)
        {
            Debug.LogError("WallStack が見つかりません。");
            return;
        }

        var v = wallStack.GetWall();
        if (v == null)
        {
            return;
        }
        v.Set(h, w);
    }
}
