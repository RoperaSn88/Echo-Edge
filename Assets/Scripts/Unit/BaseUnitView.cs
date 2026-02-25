using AndanteTribe.Utils.Unity;
using UnityEngine;

public class BaseUnitView: MonoBehaviour
{
    [SerializeField]
    private BaseUnit _baseUnit;

    [SerializeField]
    private int height;

    [SerializeField]
    private int width;

    [SerializeField]
    private int MoveHeight;

    [SerializeField]
    private int MoveWidth;

    /// <summary>
    ///  移動に使用するベクトル
    /// </summary>
    private Vector3 _moveVec;

    void Start()
    {
        // 登録用
        // 後ほどcsvで読み取る方法に変更する
        _baseUnit = new BaseUnit(this, height, width, MoveHeight, MoveWidth);
    }

    /// <summary>
    /// 左に移動するのでマイナス
    /// </summary>
    /// <param name="y">縦方向の移動量</param>
    /// <param name="x">横方向の移動量</param>
    public void Move(int y, int x)
    {
        // 横方向はマイナス方向に進めるため、負の値にする
        _moveVec.Set(-x, 0, y);
        transform.position += _moveVec;
    }
}