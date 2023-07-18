using System;
using System.Collections.Generic;
using System.Numerics;
using UnityEngine;
using UnityEngine.Serialization;
using Matrix4x4 = UnityEngine.Matrix4x4;
using Quaternion = UnityEngine.Quaternion;
using Vector3 = UnityEngine.Vector3;


[System.Serializable]
public class ShadowMapSettings
{
    [Min(0)] public float MaxDistance = 100;
    public enum TextureSize
    {
        Low = 256, Mid = 512,
        High = 1024, Epic = 2048,
    }

    [FormerlySerializedAs("resolution")] public TextureSize Resolution = TextureSize.High;
}

public class ShadowMapping
{
    
    public Frustum frustum = new Frustum();
    //配置阴影相机
    public void ConfigShadowCamera(ref Camera camera, Vector3 lightDir, int resolution)
    {
        var obb = frustum.GetOrientBoundingBox(lightDir);
        var center = (obb[0] + obb[7]) / 2;
        var width = Vector3.Magnitude(obb[0] - obb[1]);
        var height = Vector3.Magnitude(obb[0] - obb[2]);
        var len = Vector3.Magnitude(obb[0] - obb[7]);

        float DistancePrePixel = len / resolution;

        //抖动
        Matrix4x4 worldToShadow = Matrix4x4.LookAt(Vector3.zero, lightDir, Vector3.up);
        center = worldToShadow * center;
        for (int i = 0; i < 3; i++)
        {
            center[i] = Mathf.Floor(center[i] / DistancePrePixel) * DistancePrePixel;
        }
        center = worldToShadow.inverse * center;

        camera.transform.position = center;
        camera.transform.rotation = Quaternion.LookRotation(lightDir);
        camera.nearClipPlane = -500;
        camera.farClipPlane = 500;
        camera.aspect = 1.0f;
        camera.orthographicSize = len * 0.5f;
    }
    //根据相机更新视锥体
}

public class CSM
{
    //视锥划分比例
    public readonly float[] splits = { 0.10f, 0.20f, 0.30f, 0.40f };
    //正交宽度
    public readonly float[] OrthoWidths = new float[4];

    public ShadowMapping[] subShadowMapping = new ShadowMapping[4];

    public CSM()
    {
        for (int i = 0; i < 4; i++)
        {
            subShadowMapping[i] = new ShadowMapping();
        }
    }
}