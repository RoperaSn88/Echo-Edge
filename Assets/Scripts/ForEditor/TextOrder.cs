using UnityEngine;
using AndanteTribe.Utils.Unity;

public class TextOrder : MonoBehaviour
{
    [SerializeField]
    private float _range;

    [SerializeField]
    private float _angle;

    [SerializeField]
    private RectTransform[] trans;

    [Button("ボタン")]

    private void SetPos()
    {
        var basePos = ((RectTransform)transform).localPosition;
        float angle = _angle;
        foreach(var v in trans)
        {
            v.localPosition = basePos + new Vector3(_range*Mathf.Cos(Mathf.PI * angle / 180), _range*Mathf.Sin(Mathf.PI * angle / 180),0);
            angle -= _angle;
        }
    }
}
