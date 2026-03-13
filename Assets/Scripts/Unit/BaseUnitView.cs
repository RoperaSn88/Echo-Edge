using System;
using AndanteTribe.Utils.Unity;
using Cysharp.Threading.Tasks;
using DG.Tweening;
using UnityEngine;

public class BaseUnitView: MonoBehaviour, IDamageActivator
{
    [SerializeField]
    private BaseUnit _baseUnit;

    [SerializeField]
    private int height;

    [SerializeField]
    private int width;

    /// <summary>
    ///  移動に使用するベクトル
    /// </summary>
    private Vector3 _moveVec;

    /// <summary>
    /// 登録用。本番ではcsv読み取りへ変更し、削除
    /// </summary>
    [SerializeField]
    private BaseStatus _baseStatus;

    private const float MoveTime = 0.15f;

    void Start()
    {
        // 登録用
        // 後ほどcsvで読み取る方法に変更する
        _baseUnit = new BaseUnit(this, height, width);
        _baseStatus.RegistarBuff(new HPBuff());
        _baseUnit.RegistarStatus(_baseStatus);
    }

    /// <summary>
    /// 左に移動するのでマイナス
    /// </summary>
    /// <param name="y">縦方向の移動量</param>
    /// <param name="x">横方向の移動量</param>
    public async UniTask Move(int y, int x)
    {
        // 横方向はマイナス方向に進めるため、負の値にする
        _moveVec = new Vector3(x, 0, y);
        await transform.DOLocalMove(_moveVec, MoveTime).SetEase(Ease.OutQuad);
        await UniTask.Delay(TimeSpan.FromSeconds(MoveTime * 3f));
        height = y;
        width = x;
    }

    public (int damage, bool isDeath) Damage()
    {
        BattleManager.RegistarEnemy(MapManager.Instance.GetUnitAt(height, width).GetStatus());
        return BattleManager.EnemyDamage();
    }
}