using UnityEngine;

public struct CameraSettings
{
    public Vector3 position;
    public Quaternion rotation;
    public float nearClipPlane, farClipPlane, aspect;
}

public class ShadowMapping
{
    private CameraSettings _cameraSettings;
    private Frustum _frustum = new Frustum();
    private Vector3[] _obb = new Vector3[8];

    //保存渲染相机设置，改为光源相机
    public void SaveRenderingCameraSettings(ref Camera Camera)
    {
        _cameraSettings.position = Camera.transform.position;
        _cameraSettings.rotation = Camera.transform.rotation;
        _cameraSettings.farClipPlane = Camera.farClipPlane;
        _cameraSettings.nearClipPlane = Camera.nearClipPlane;
        _cameraSettings.aspect = Camera.aspect;
        Camera.orthographic = true;
    }

    //恢复渲染相机设置
    public void RevertRenderingCameraSettings(ref Camera camera)
    {
        camera.transform.position = _cameraSettings.position;
        camera.transform.rotation = _cameraSettings.rotation;
        camera.farClipPlane = _cameraSettings.farClipPlane;
        camera.nearClipPlane = _cameraSettings.nearClipPlane;
        camera.aspect = _cameraSettings.aspect;
        camera.orthographic = false;
    }

    //配置阴影相机
    public void ConfigShadowCamera(ref Camera camera, Vector3 lightDir, float distance)
    {
        _obb = _frustum.GetOrientBoundingBox(lightDir);

        var center = (_obb[0] + _obb[3]) / 2;
        var width = Vector3.Magnitude(_obb[0] - _obb[1]);
        var height = Vector3.Magnitude(_obb[0] - _obb[2]);
        var orthographicSize = Mathf.Max(width, height);

        camera.transform.rotation = Quaternion.LookRotation(lightDir);
        camera.transform.position = center;
        camera.nearClipPlane = -distance;
        camera.farClipPlane = distance;
        camera.aspect = 1;
        camera.orthographicSize = orthographicSize / 2;
        
        _frustum.DrawOBB(_obb, Color.red);
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
            camera.farClipPlane,
            Camera.MonoOrStereoscopicEye.Mono,
            _frustum.FarCorners
        );
        
        //变换视锥体到世界空间
        for (var i = 0; i < 4; i++)
        {
            _frustum.NearCorners[i] = camera.transform.TransformVector(_frustum.NearCorners[i]) + camera.transform.position;
            _frustum.FarCorners[i] = camera.transform.TransformVector(_frustum.FarCorners[i]) + camera.transform.position;
        }
        
        _frustum.DrawFrustum(Color.blue);
    }
}
