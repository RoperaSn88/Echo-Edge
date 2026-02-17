using UnityEngine;
using UnityEngine.EventSystems;

[System.Serializable]
public class ClickPanel : MonoBehaviour, IPointerUpHandler, IPointerDownHandler
{
    /// <summary>
    /// ポインターデリゲート
    /// </summary>
    public delegate void PointerAction();

    /// <summary>
    /// 押したときのデリゲート
    /// </summary>
    public event PointerAction upActions;

    /// <summary>
    /// 離したときのデリゲート
    /// </summary>
    public event PointerAction downActions;

    /// <summary>
    /// 押したボタンの種類のフィールド
    /// </summary>
    public PointerEventData.InputButton clickID;

    /// <summary>
    /// マウスをおしたときに発火させる。
    /// デリゲート実行されるだけでいい。
    /// </summary>
    /// <param name="pointerEventData"></param>
    public void OnPointerDown(PointerEventData pointerEventData)
    {   
        clickID = pointerEventData.button;
        downActions?.Invoke();
    }

    /// <summary>
    /// マウスを離したときに発火される。
    /// デリゲートだけでいいわ。
    /// </summary>
    /// <param name="pointerEventData"></param>
    public void OnPointerUp(PointerEventData pointerEventData)
    {
        clickID = pointerEventData.button;
        upActions?.Invoke();
    }

    /// <summary>
    /// 押したときの処理をリセットする。
    /// </summary>
    public void ResetPointerUp()
    {
        upActions = null;
    }

    /// <summary>
    /// 離したときの処理をリセットする。
    /// </summary>
    public void ResetPointerDown()
    {
        downActions = null;
    }


}
