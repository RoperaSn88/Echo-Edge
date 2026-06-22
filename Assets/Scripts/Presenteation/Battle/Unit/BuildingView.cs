using UnityEngine;

public class BuildingView : MonoBehaviour
{
    public void Set(int h, int w)
    {
        Vector3 vec = new Vector3(w, 0.25f, h);
        transform.localPosition = vec;
    }
}
