using UnityEngine;
using AndanteTribe.Utils.Unity;

public class MapView : MonoBehaviour
{
    private MapManager _manager;

    void Start()
    {
        _manager = new MapManager();
    }

    [Button("動かす")]
    void MoveUnit()
    {
        _manager.MoveUnit();
    }
}
