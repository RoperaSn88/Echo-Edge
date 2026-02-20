using UnityEngine;

public class MapView : MonoBehaviour
{
    private MapManager _manager;

    void Start()
    {
        _manager = new MapManager();
    }
}
