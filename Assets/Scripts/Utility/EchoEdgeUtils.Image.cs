using UnityEngine;

namespace EchoEdge.Utils
{
    public static class EchoEdgeUtils
    {
        public static void SetImageAlpha(this UnityEngine.UI.Image image, float alpha)
        {
            var color = image.color;
            color.a = alpha;
            image.color = color;
        }

        public static void SetImageBrightness(this UnityEngine.UI.Image image, float brightness)
        {
            var color = image.color;
            color.r = brightness;
            color.g = brightness;
            color.b = brightness;
            image.color = color;
        }
    }
}
