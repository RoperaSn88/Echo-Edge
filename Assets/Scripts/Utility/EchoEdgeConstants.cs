using UnityEngine;

namespace EchoEdge.Utility
{
    public static class EchoEdgeConstants
    {
        public static Color RedColor()
        {
            ColorUtility.TryParseHtmlString("#E67A65", out var RedColor);
            return RedColor;
        }

        public static Color BlueColor()
        {
            ColorUtility.TryParseHtmlString("#6593E6", out var BlueColor);
            return BlueColor;
        }

        public static void SetAlpha(ref Color color, float alpha)
        {
            color.a = alpha;
        }
    }
}
