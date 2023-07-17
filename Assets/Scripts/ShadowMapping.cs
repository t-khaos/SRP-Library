using System;
using System.Collections.Generic;
using UnityEngine;


[System.Serializable]
public class ShadowMapSettings
{
    [Min(0)] public float MaxDistance = 100;
}

public class ShadowMapping
{
    public Frustum frustum = new Frustum();
    //配置阴影相机
    public void ConfigShadowCamera(ref Camera camera, Vector3 lightDir)
    {
        var obb = frustum.GetOrientBoundingBox(lightDir);
        var center = (obb[0] + obb[7]) / 2;
        var width = Vector3.Magnitude(obb[0] - obb[1]);
        var height = Vector3.Magnitude(obb[0] - obb[2]);
        var len = Mathf.Max(width, height);


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
    public readonly float[] splits = { 0.07f, 0.13f, 0.25f, 0.55f };
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