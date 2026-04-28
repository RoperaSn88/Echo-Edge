using UnityEngine;
using UnityEngine.EventSystems;

/// <summary>
/// クリックイベントを通知するデリゲートの型
/// </summary>
public delegate void ClickAction(PointerEventData eventData);

/// <summary>
/// クリックイベントを持つオブジェクトのインターフェース
/// </summary>
public interface IClickableImage
{
    event ClickAction OnClick;
}

/// <summary>
/// IPointerClickHandlerを実装し、クリック時にOnClickイベントを発火するスクリプト。
/// Imageオブジェクトに追加して使用する。
/// </summary>
public class ClickableImage : MonoBehaviour, IPointerClickHandler, IClickableImage
{
    /// <summary>
    /// クリック時に発火するイベント
    /// </summary>
    public event ClickAction OnClick;

    /// <summary>
    /// クリック時に実行される処理
    /// </summary>
    public void OnPointerClick(PointerEventData eventData)
    {
        OnClick?.Invoke(eventData);
    }
}
