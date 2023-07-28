using UnityEngine;

public static class DebugExtension
{
    public static void DrawFrustum(this Frustum frustum, Color color)
    {
        for (int j = 3, i = 0; i < 4; i++, j = i - 1)
        {
            Debug.DrawLine(frustum.nearCorners[i], frustum.farCorners[i], color);
            Debug.DrawLine(frustum.nearCorners[j], frustum.nearCorners[i], color);
            Debug.DrawLine(frustum.farCorners[j], frustum.farCorners[i], color);
        }
    }

    public static void DrawFrustumOrientBoundingBox(this Frustum frustum, Vector3 lightDir, Color color)
    {
        //      6--------7
        //  2--------3   |
        //  |   |    |   |
        //  |   4----|---5
        //  0--------1
        var bounds = frustum.GetOrientBoundingBox(lightDir);
        DrawOrientBoundingBox(bounds, color);
    }

    public static void DrawOrientBoundingBox(Vector3[] bounds, Color color)
    {
        //      6--------7
        //  2--------3   |
        //  |   |    |   |
        //  |   4----|---5
        //  0--------1
        for (int i = 0; i < 4; i++)
        {
            // 绘制底部矩形
            //0,1,2,3 - 4,5,6,7
            Debug.DrawLine(bounds[i], bounds[i + 4], color);
            //0,2,4,6 - 1,3,5,7
            Debug.DrawLine(bounds[i * 2], bounds[i * 2 + 1], color);
            //0,1,4,5 - 2,3,6,7
            Debug.DrawLine(bounds[i + (i / 2) * 2], bounds[i + (i / 2) * 2 + 2], color);
        }
    }
    public static void DrawOrientBoundingBox(Vector4[] bounds, Color color)
    {
        //      6--------7
        //  2--------3   |
        //  |   |    |   |
        //  |   4----|---5
        //  0--------1
        for (int i = 0; i < 4; i++)
        {
            // 绘制底部矩形
            //0,1,2,3 - 4,5,6,7
            Debug.DrawLine(bounds[i], bounds[i + 4], color);
            //0,2,4,6 - 1,3,5,7
            Debug.DrawLine(bounds[i * 2], bounds[i * 2 + 1], color);
            //0,1,4,5 - 2,3,6,7
            Debug.DrawLine(bounds[i + (i / 2) * 2], bounds[i + (i / 2) * 2 + 2], color);
        }
    }
}