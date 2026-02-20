using AndanteTribe.Utils.Unity;
using UnityEngine;

public class BaseUnitView: MonoBehaviour
{
    private BaseUnit _baseUnit;

    [SerializeField]
    private int height;

    [SerializeField]
    private int width;

    [SerializeField]
    private int MoveHeight;

    [SerializeField]
    private int MoveWidth;

    void Start()
    {
        _baseUnit = new BaseUnit(this, height, width, MoveHeight, MoveWidth);
    }

    public void Move()
    {
        transform.position += Vector3.one;
    }
}