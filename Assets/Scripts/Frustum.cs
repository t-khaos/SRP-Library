using System.Collections.Generic;
using UnityEngine;

public class Frustum
{
    public Vector3[] farCorners = new Vector3[4];
    public Vector3[] nearCorners = new Vector3[4];

    public void InitByCamera(Camera camera, float near, float far)
    {
        //求出相机空间视锥体坐标
        camera.CalculateFrustumCorners(
            new Rect(0, 0, 1, 1),
            near,
            Camera.MonoOrStereoscopicEye.Mono,
            nearCorners
        );
        camera.CalculateFrustumCorners(
            new Rect(0, 0, 1, 1),
            far,
            Camera.MonoOrStereoscopicEye.Mono,
            farCorners
        );

        //变换视锥体到世界空间
        for (var i = 0; i < 4; i++)
        {
            nearCorners[i] = camera.transform.TransformVector(nearCorners[i]) + camera.transform.position;
            farCorners[i] = camera.transform.TransformVector(farCorners[i]) + camera.transform.position;
        }
    }

    public Vector3[] GetOrientBoundingBox(Vector3 direction)
    {
        var OrientToWorld = Matrix4x4.LookAt(Vector3.zero, direction, Vector3.up);
        var WorldToOrient = OrientToWorld.inverse;


        var corners = new List<Vector3>(8);
        for (var i = 0; i < 4; i++)
        {
            corners.Add(WorldToOrient * nearCorners[i]);
            corners.Add(WorldToOrient * farCorners[i]);
        }

        var size = new Vector3[2];
        size[0] = size[1] = corners[0];
        for (var i = 1; i < corners.Count; i++)
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
            bounds[i] = OrientToWorld * new Vector3(
                size[(i & 1) >> 0].x,
                size[(i & 2) >> 1].y,
                size[(i & 4) >> 2].z
            );
        }

        return bounds;
    }

    public Vector4 GetPlane(Vector3 normal, Vector3 point)
    {
        return new Vector4(normal.x, normal.y, normal.z, -Vector3.Dot(normal, point));
    }

    private Vector4 GetPlane(Vector3 a, Vector3 b, Vector3 c)
    {
        var normal = Vector3.Normalize(Vector3.Cross(b - a, c - a));
        return GetPlane(normal, a);
    }

    public Vector4[] GetFrustumPlane()
    {
        var planes = new Vector4[6];

        // Near plane
        planes[0] = GetPlane(nearCorners[0], nearCorners[2], nearCorners[1]);

        // Far plane
        planes[1] = GetPlane(farCorners[0], farCorners[1], farCorners[2]);

        // Left plane
        planes[2] = GetPlane(nearCorners[0], farCorners[0], nearCorners[2]);

        // Right plane
        planes[3] = GetPlane(nearCorners[1], nearCorners[3], farCorners[1]);

        // Top plane
        planes[4] = GetPlane(nearCorners[2], farCorners[2], farCorners[3]);

        // Bottom plane
        planes[5] = GetPlane(nearCorners[0], nearCorners[1], farCorners[0]);

        return planes;
    }
}