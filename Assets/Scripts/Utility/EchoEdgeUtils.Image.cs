using UnityEngine;

namespace EchoEdge.Utility
{
    public static class EchoEdgeUtils
    {
        public static void SetImageAlpha(this UnityEngine.UI.Image image, float alpha)
        {
            var color = image.color;
            color.a = alpha;
            image.color = color;
        }
    }
}