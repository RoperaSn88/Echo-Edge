using UnityEngine;
using System.Collections.Generic;

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

    [SerializeField, Min(0)]
    private int initialPoolSize = 8;

    private readonly Stack<BaseUnitView> _unitViewPool = new();

    void Awake()
    {
        Instance = this;
        SetupPool();
    }

    /// <summary>
    /// BaseUnit の位置に BaseUnitView を持つオブジェクトを生成し、unit に紐づける
    /// </summary>
    /// <param name="unit">配置済みの BaseUnit</param>
    public void SpawnView(BaseUnit unit, EnemyKinds enemyId)
    {
        var view = GetPooledView();
        if (view == null)
        {
            return;
        }

        view.Setup(unit.Height, unit.Width, enemyId);
        unit.SetView(view);
    }

    public void ReturnView(BaseUnitView view)
    {
        if (view == null) return;
        view.gameObject.SetActive(false);
        _unitViewPool.Push(view);
    }

    private void SetupPool()
    {
        for (int i = 0; i < initialPoolSize; i++)
        {
            var view = CreatePooledView();
            if (view != null)
            {
                _unitViewPool.Push(view);
            }
        }
    }

    private BaseUnitView GetPooledView()
    {
        while (_unitViewPool.Count > 0)
        {
            var pooledView = _unitViewPool.Pop();
            if (pooledView != null)
            {
                pooledView.gameObject.SetActive(true);
                return pooledView;
            }
        }

        var view = CreatePooledView();
        if (view != null)
        {
            view.gameObject.SetActive(true);
        }
        return view;
    }

    private BaseUnitView CreatePooledView()
    {
        if (unitPrefab == null || !unitPrefab.TryGetComponent<BaseUnitView>(out _))
        {
            Debug.LogError("unitPrefab に BaseUnitView がアタッチされていません。");
            return null;
        }

        var obj = Instantiate(unitPrefab, unitsParent);
        if (!obj.TryGetComponent<BaseUnitView>(out var view))
        {
            Debug.LogError("unitPrefab に BaseUnitView がアタッチされていません。");
            return null;
        }

        obj.SetActive(false);
        return view;
    }
}
