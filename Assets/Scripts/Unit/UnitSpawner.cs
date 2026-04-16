using UnityEngine;

/// <summary>
/// ユニットをインスタンス化して配置するスポナー
/// </summary>
public class UnitSpawner : MonoBehaviour
{
    public static UnitSpawner Instance;

    [SerializeField]
    private GameObject unitPrefab;

    [SerializeField]
    private Transform unitsParent;

    void Awake()
    {
        Instance = this;
    }

    /// <summary>
    /// BaseUnit の位置に BaseUnitView を持つオブジェクトを生成し、unit に紐づける
    /// </summary>
    /// <param name="unit">配置済みの BaseUnit</param>
    public void SpawnView(BaseUnit unit)
    {
        // 後でエネミープールで敵を管理する
        GameObject obj = Instantiate(unitPrefab, unitsParent);

        if (!obj.TryGetComponent<BaseUnitView>(out var view))
        {
            Debug.LogError("unitPrefab に BaseUnitView がアタッチされていません。");
            Destroy(obj);
            return;
        }

        view.Setup(unit.Height, unit.Width);
        unit.SetView(view);
    }
}
