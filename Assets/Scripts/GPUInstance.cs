using System;
using UnityEngine;
using UnityEngine.Serialization;
using Random = UnityEngine.Random;

public class GPUInstance : MonoBehaviour
{
    private ComputeBuffer matrixBuffer;

    public int InstanceCount = 1000;
    public Mesh InstanceMesh;
    public Material instanceMatrial;
    public int subMeshIdx = 0;

    //索引数量 - 实例数量 - 第一个索引的idx - 第一个顶点的idx - 第一个实例的位置
    private uint[] args = new uint[5] { 0, 0, 0, 0, 0 };
    private ComputeBuffer argsBuffer;

    private int cachedInstanceCount = -1;
    private int cachedSubMeshIdx = -1;

    public bool EnableFrustumCulling = false;

    //视锥剔除
    public ComputeShader cs;
    private CullingInfo cullingInfo;
    private int kernel;

    public Camera camera;

    private Matrix4x4[] localToWorldMatrices;

    public bool EnableDebug = false;

    void Start()
    {
        argsBuffer = new ComputeBuffer(1, args.Length * sizeof(uint), ComputeBufferType.IndirectArguments);
        cullingInfo = new CullingInfo(cs, InstanceCount);
        kernel = cs.FindKernel("CSMain");
    }

    // Update is called once per frame
    void Update()
    {
        subMeshIdx = Math.Clamp(subMeshIdx, 0, InstanceMesh.subMeshCount - 1);

        UpdateMatrices();
        UpdateArgs();
        UpdateCaches();

        //视锥剔除
        if (EnableFrustumCulling)
        {
            Culling();
            //更新实例数量为剔除后的数量
            ComputeBuffer.CopyCount(cullingInfo.CullingResult, argsBuffer, sizeof(uint));
        }
        //不剔除则直接传给顶点着色器
        else
        {
            instanceMatrial.SetBuffer("_ObjectToWorldMatrices", matrixBuffer);
        }

        //绘制Instance
        Graphics.DrawMeshInstancedIndirect(
            InstanceMesh, subMeshIdx, instanceMatrial,
            new Bounds(Vector3.zero, new Vector3(200.0f, 200.0f, 200.0f)),
            argsBuffer
        );
    }


    void UpdateMatrices()
    {
        if (cachedInstanceCount == InstanceCount) return;
        //初始化矩阵缓冲
        matrixBuffer?.Release();
        matrixBuffer = new ComputeBuffer(InstanceCount, sizeof(float) * 16);
        //随机生成模型变换矩阵
        localToWorldMatrices = new Matrix4x4[InstanceCount];
        for (var i = 0; i < InstanceCount; i++)
        {
            var angle = Random.Range(0.0f, Mathf.PI * 2.0f);
            var distance = Random.Range(10.0f, 100.0f);
            var position = new Vector4(Mathf.Sin(angle) * distance, 1, Mathf.Cos(angle) * distance, 1);
            localToWorldMatrices[i] = Matrix4x4.identity;
            localToWorldMatrices[i].SetColumn(3, new Vector4(position.x, position.y, position.z, 1));
        }

        matrixBuffer.SetData(localToWorldMatrices);
    }

    void UpdateArgs()
    {
        if (cachedSubMeshIdx == subMeshIdx) return;
        //设置args buffer
        if (InstanceMesh)
        {
            args[0] = InstanceMesh.GetIndexCount(subMeshIdx);
            args[1] = (uint)InstanceCount;
            args[2] = InstanceMesh.GetIndexStart(subMeshIdx);
            args[3] = InstanceMesh.GetBaseVertex(subMeshIdx);
        }
        else
        {
            args[0] = args[1] = args[2] = args[3] = 0;
        }

        argsBuffer.SetData(args);
    }

    void UpdateCaches()
    {
        cachedInstanceCount = InstanceCount;
        cachedSubMeshIdx = subMeshIdx;
    }

    private void Culling()
    {
        if (!EnableFrustumCulling) return;

        cs.SetBuffer(kernel, "_matrixBuffer", matrixBuffer);

        cs.SetBuffer(kernel, "_argsBuffer", argsBuffer);

        //初始化剔除后矩阵buffer
        cs.SetBuffer(kernel, "_validMatrixBuffer", cullingInfo.CullingResult);
        cullingInfo.CullingResult.SetCounterValue(0);

        //更新实例数量，传入cs
        cs.SetInt("_instanceCount", InstanceCount);

        //更新视锥平面，传入cs
        cullingInfo.CullingFrustum.InitByCamera(camera, camera.nearClipPlane, camera.farClipPlane);
        var planes = GeometryUtility.CalculateFrustumPlanes(camera);
        var planeVector4s = new Vector4[6];
        for (int i = 0; i < planes.Length; i++)
        {
            planeVector4s[i] = new Vector4(planes[i].normal.x, planes[i].normal.y, planes[i].normal.z, planes[i].distance);
        }

        cs.SetVectorArray("_planes", planeVector4s);

        //计算mesh的包围盒，传入cs
        var bounds = ConvertBoundsToVector4Array(InstanceMesh.bounds);
        cs.SetVectorArray("_bounds", bounds);

        //debug
        if (EnableDebug)
        {
            //视锥

            //包围盒
            for (int i = 0; i < InstanceCount; i++)
            {
                var instanceBounds = new Vector4[8];
                Array.Copy(bounds, instanceBounds, bounds.Length);
                for (int j = 0; j < 8; j++)
                {
                    instanceBounds[j] = localToWorldMatrices[i] * bounds[j];
                }

                var vis = Visibility(planeVector4s, instanceBounds);
                DebugExtension.DrawOrientBoundingBox(instanceBounds, vis ? Color.green : Color.red);
            }
        }

        //执行剔除的计算着色器
        var dispatchNum = InstanceCount / 128 + 1;
        cs.Dispatch(kernel, dispatchNum, 1, 1);

        //将剔除后的模型变换矩阵传入顶点着色器
        instanceMatrial.SetBuffer("_ObjectToWorldMatrices", cullingInfo.CullingResult);
    }

    private void OnDisable()
    {
        matrixBuffer?.Release();
        matrixBuffer = null;

        argsBuffer?.Release();
        argsBuffer = null;

        if (!EnableFrustumCulling) return;
        cullingInfo.CullingResult?.Release();
        cullingInfo.CullingResult = null;
    }

    private Vector4[] ConvertBoundsToVector4Array(Bounds bounds)
    {
        var min = bounds.min;
        var max = bounds.max;

        var corners = new Vector4[8];

        for (var i = 0; i < 8; i++)
        {
            corners[i] = new Vector4(
                (i & 1) == 0 ? min.x : max.x,
                (i & 2) == 0 ? min.y : max.y,
                (i & 4) == 0 ? min.z : max.z,
                1
            );
        }

        return corners;
    }

    private Vector3[] ConvertBoundsToVector3Array(Bounds bounds)
    {
        var min = bounds.min;
        var max = bounds.max;

        var corners = new Vector3[8];

        for (var i = 0; i < 8; i++)
        {
            corners[i] = new Vector3(
                (i & 1) == 0 ? min.x : max.x,
                (i & 2) == 0 ? min.y : max.y,
                (i & 4) == 0 ? min.z : max.z
            );
        }

        return corners;
    }

    int GetSide(Vector4 plane, Vector3 p)
    {
        return Vector3.Dot(p, plane) + plane.w > 0 ? 1 : 0;
    }

    int IsInside(Vector4[] planes, Vector3 p)
    {
        int cnt = 0;
        for (int i = 0; i < 6; i++)
            cnt += GetSide(planes[i], p);
        return cnt == 6 ? 1 : 0;
    }

    bool Visibility(Vector4[] planes, Vector4[] bounds)
    {
        int cnt = 0;
        for (int i = 0; i < 8; i++)
            cnt += IsInside(planes, bounds[i]);
        return cnt > 0;
    }
}