using UnityEngine;

public class BuildingManager : MonoBehaviour
{
    public static BuildingManager Instance;
    [SerializeField]
    private GameObject building;

    [SerializeField]
    private Transform enemys;

    void Start()
    {
        Instance = this;
    }

    public void SetBuilding(int h, int w)
    {
        GameObject b = Instantiate(building, enemys);
        b.TryGetComponent<BuildingView>(out var v);
        v.Set(h, w);
    }
}