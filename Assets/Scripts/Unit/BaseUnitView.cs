using AndanteTribe.Utils.Unity;
using UnityEngine;

public class BaseUnitView: MonoBehaviour
{
    private BaseUnit _baseUnit;

    [SerializeField]
    private int height;

    [SerializeField]
    private int width;

    void Start()
    {
        _baseUnit = new BaseUnit(height, width);
    }
    
    [Button("Move")]
    void Move()
    {
        MapManager.Instance.TryMoveUnit(_baseUnit.Height, _baseUnit.Width, _baseUnit.Height, _baseUnit.Width - 1);
        transform.position += Vector3.one;
    }
}