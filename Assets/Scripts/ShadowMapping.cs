using System;
using UnityEngine;

public struct CameraSettings
{
    public Vector3 position;
    public Quaternion rotation;
    public float farClipPlane, nearClipPlane, aspect;
}

public class ShadowMapping
{
    private CameraSettings settings;
    private Frustum _frustum = new Frustum();
    private Vector3[] _obb = new Vector3[8];

    //保存渲染相机设置，改为光源相机
    public void SaveRenderingCameraSettings(ref Camera camera)
    {
        settings.position = camera.transform.position;
        settings.rotation = camera.transform.rotation;
        settings.farClipPlane = camera.farClipPlane;
        settings.nearClipPlane = camera.nearClipPlane;
        settings.aspect = camera.aspect;
        camera.orthographic = true;
    }

    //恢复渲染相机设置
    public void RevertRenderingCameraSettings(ref Camera camera)
    {
        camera.transform.position = settings.position;
        camera.transform.rotation = settings.rotation;
        camera.farClipPlane = settings.farClipPlane;
        camera.nearClipPlane = settings.nearClipPlane;
        camera.aspect = settings.aspect;
        camera.orthographic = false;
    }

    //配置阴影相机
    public void ConfigShadowCamera(ref Camera camera, Vector3 lightDir)
    {
        var center = (_obb[0] + _obb[7]) / 2;
        var width = Vector3.Magnitude(_obb[0] - _obb[1]);
        var height = Vector3.Magnitude(_obb[0] - _obb[2]);

        var len = Mathf.Max(width, height);
        
        camera.transform.rotation = Quaternion.LookRotation(lightDir);
        camera.transform.position = center;
        camera.nearClipPlane = -500;
        camera.farClipPlane = 500;
        camera.aspect = 1.0f;
        camera.orthographicSize = len * 0.5f;
    }

    public void UpdateFrustem(Camera camera, Vector3 lightDir)
    {
        //求出相机空间视锥体坐标
        camera.CalculateFrustumCorners(
            new Rect(0, 0, 1, 1),
            camera.nearClipPlane,
            Camera.MonoOrStereoscopicEye.Mono,
            _frustum.NearCorners
        );
        camera.CalculateFrustumCorners(
            new Rect(0, 0, 1, 1),
            10,
            Camera.MonoOrStereoscopicEye.Mono,
            _frustum.FarCorners
        );

        //变换视锥体到世界空间
        for (var i = 0; i < 4; i++)
        {
            _frustum.NearCorners[i] = camera.transform.TransformVector(_frustum.NearCorners[i]) + camera.transform.position;
            _frustum.FarCorners[i] = camera.transform.TransformVector(_frustum.FarCorners[i]) + camera.transform.position;
        }

        _obb = _frustum.GetOrientBoundingBox(lightDir);
    }

    public void DebugFrustem()
    {
        _frustum.DrawFrustum(Color.blue);
        _frustum.DrawOBB(_obb, Color.red);
    }
}