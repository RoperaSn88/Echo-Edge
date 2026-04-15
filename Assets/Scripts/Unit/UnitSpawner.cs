using Cysharp.Threading.Tasks;
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
    /// 指定した enemyId・座標にユニットを生成し、CSV からステータスを反映する
    /// </summary>
    /// <param name="enemyId">EnemyInfo.csv の ID</param>
    /// <param name="h">配置する縦座標</param>
    /// <param name="w">配置する横座標</param>
    public async UniTask SpawnUnit(int enemyId, int h, int w)
    {
        GameObject obj = Instantiate(unitPrefab, unitsParent);

        if (!obj.TryGetComponent<BaseUnitView>(out var view))
        {
            Debug.LogError("unitPrefab に BaseUnitView がアタッチされていません。");
            return;
        }

        var status = new BattleStatus();
        bool loaded = await EnemyStatusLoader.TryLoad(enemyId, status);
        if (!loaded)
        {
            Debug.LogWarning($"enemyId {enemyId} のステータスを読み込めなかったため、ユニットを配置しませんでした。");
            Destroy(obj);
            return;
        }

        view.Setup(h, w, status);
    }
}
