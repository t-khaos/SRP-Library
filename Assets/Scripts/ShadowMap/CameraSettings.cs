using UnityEngine;

public class CameraSettings
{
    private Vector3 position;
    private Quaternion rotation;
    private float farClipPlane = 100.0f;
    private float nearClipPlane = 0.3f;
    private float aspect;

    public void Save(ref Camera camera, bool orthoraphic = true)
    {
        position = camera.transform.position;
        rotation = camera.transform.rotation;
        farClipPlane = camera.farClipPlane;
        nearClipPlane = camera.nearClipPlane;
        aspect = camera.aspect;
        camera.orthographic = orthoraphic;
    }

    //恢复渲染相机设置
    public void Revert(ref Camera camera, bool orthoraphic = false)
    {
        camera.transform.position = position;
        camera.transform.rotation = rotation;
        camera.farClipPlane = farClipPlane;
        camera.nearClipPlane = nearClipPlane;
        camera.aspect = aspect;
        camera.orthographic = orthoraphic;
    }
}