
using System.Collections.Generic;
using UnityEngine;

public class Frustum
{
    public Vector3[] FarCorners = new Vector3[4];
    public Vector3[] NearCorners = new Vector3[4];
        
    public void DrawFrustum(Color color)
    {
        for (int j = 3, i = 0; i < 4; i++, j=i-1)
        {
            Debug.DrawLine(NearCorners[i], FarCorners[i], color);
            Debug.DrawLine(NearCorners[j], NearCorners[i], color);
            Debug.DrawLine(FarCorners[j], FarCorners[i], color);
        }
    }

    public void DrawOBB(Vector3[] bounds, Color color)
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
            Debug.DrawLine(bounds[i], bounds[i+4], color);
            //0,2,4,6 - 1,3,5,7
            Debug.DrawLine(bounds[i*2], bounds[i*2+1], color);
            //0,1,4,5 - 2,3,6,7
            Debug.DrawLine(bounds[i+(i/2)*2], bounds[i+(i/2)*2+2], color); 
        }
        
        
        Debug.DrawLine((bounds[0]+bounds[3])/2, bounds[0]);
    }
    

    public Vector3[] GetOrientBoundingBox(Vector3 direction)
    {

        var OrientToWorld = Matrix4x4.LookAt(Vector3.zero, direction, Vector3.up);
        var WorldToOrient= OrientToWorld.inverse;
            

        var corners = new List<Vector3>(8);
        for (var i = 0; i < 4; i++)
        {
            corners.Add(WorldToOrient * NearCorners[i]);
            corners.Add(WorldToOrient * FarCorners[i]);
        }
        var size = new Vector3[2];
        size[0] = size[1] = corners[0];
        for (var i = 1;i<corners.Count;i++)
        {
            size[0] = Vector3.Min(size[0], corners[i]);
            size[1] = Vector3.Max(size[1], corners[i]);
        }
        
        //根据边界构建包围盒八个点，并转换到世界空间
        //   | 0 | 1 | 2 | 3 | 4 | 5 | 6 | 7 
        //---|---|---|---|---|---|---|---|---
        //  x|min|max|min|max|min|max|min|max
        //  y|min|min|max|max|min|min|max|max
        //  z|min|min|min|min|max|max|max|max
        var bounds = new Vector3[8];
        for (var i = 0; i < 8; i++)
        {
            bounds[i] =OrientToWorld *new Vector3(
                size[(i & 1) >> 0].x, 
                size[(i & 2) >> 1].y, 
                size[(i & 4) >> 2].z
            );
        }

        return bounds;
    }
}