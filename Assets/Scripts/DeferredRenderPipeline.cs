using UnityEditor;
using UnityEngine;
using UnityEngine.Profiling;
using UnityEngine.Rendering;

public class DeferredRenderPipeline : RenderPipeline
{
    RenderTexture gdepth;
    RenderTexture[] gbuffers = new RenderTexture[4];
    RenderTargetIdentifier[] gbufferID = new RenderTargetIdentifier[4];

    public Cubemap DiffuseIBL;
    public Cubemap SpecularIBL;
    public Texture BRDFLUT;

    //阴影相关
    //private RenderTexture shadowTex;
    private RenderTexture[] shadowTextures = new RenderTexture[4];

    private RenderTexture shadowStrengthTex;

    private ShadowMapSettings shadowMapSettings = new ShadowMapSettings();
    private ShadowMapping shadowMapping = new ShadowMapping();
    private CSM csm = new CSM();

    public DeferredRenderPipeline()
    {
        //初始化GBuffer
        gdepth = new RenderTexture(Screen.width, Screen.height, 24, RenderTextureFormat.Depth, RenderTextureReadWrite.Linear);
        gbuffers[0] = new RenderTexture(Screen.width, Screen.height, 0, RenderTextureFormat.ARGB32, RenderTextureReadWrite.sRGB);
        gbuffers[1] = new RenderTexture(Screen.width, Screen.height, 0, RenderTextureFormat.ARGB32, RenderTextureReadWrite.Linear);
        gbuffers[2] = new RenderTexture(Screen.width, Screen.height, 0, RenderTextureFormat.ARGB32, RenderTextureReadWrite.Linear);
        gbuffers[3] = new RenderTexture(Screen.width, Screen.height, 0, RenderTextureFormat.ARGB32, RenderTextureReadWrite.Linear);

        for (int i = 0; i < 4; i++)
            gbufferID[i] = gbuffers[i];

        //初始化阴影RT
        //shadowTex = new RenderTexture(shadowMapSize, shadowMapSize, 24, RenderTextureFormat.Depth, RenderTextureReadWrite.Linear);
        shadowStrengthTex = new RenderTexture(Screen.width, Screen.height, 0, RenderTextureFormat.R8, RenderTextureReadWrite.Linear);
        for (var i = 0; i < shadowTextures.Length; i++)
        {
            shadowTextures[i] = new RenderTexture((int)shadowMapSettings.Resolution, (int)shadowMapSettings.Resolution, 24, RenderTextureFormat.Depth, RenderTextureReadWrite.Linear);
        }
    }

    protected override void Render(ScriptableRenderContext context, Camera[] cameras)
    {
        if (cameras.Length == 0) return;
        Camera camera = cameras[0];
        context.SetupCameraProperties(camera);

        //设置Shader全局变量
        Shader.SetGlobalFloat("_far", camera.farClipPlane);
        Shader.SetGlobalFloat("_near", camera.nearClipPlane);
        Shader.SetGlobalFloat("_screenWidth", Screen.width);
        Shader.SetGlobalFloat("_screenHeight", Screen.height);

        //GBuffer相关RT
        Shader.SetGlobalTexture("_gdepth", gdepth);
        for (int i = 0; i < 4; i++)
            Shader.SetGlobalTexture("_GT" + i, gbuffers[i]);

        //VP矩阵
        Matrix4x4 viewMatrix = camera.worldToCameraMatrix;
        Matrix4x4 projMatrix = GL.GetGPUProjectionMatrix(camera.projectionMatrix, false);
        Matrix4x4 vpMatrix = projMatrix * viewMatrix;
        Matrix4x4 vpMatrixInv = vpMatrix.inverse;
        Shader.SetGlobalMatrix("_vpMatrix", vpMatrix);
        Shader.SetGlobalMatrix("_vpMatrixInv", vpMatrixInv);

        //IBL相关贴图
        Shader.SetGlobalTexture("_DiffuseIBL", DiffuseIBL);
        Shader.SetGlobalTexture("_SpecularIBL", SpecularIBL);
        Shader.SetGlobalTexture("_BRDFLUT", BRDFLUT);

        //阴影相关RT
        for (int i = 0; i < shadowTextures.Length; i++)
        {
            Shader.SetGlobalTexture("_ShadowMap" + i, shadowTextures[i]);
            Shader.SetGlobalFloat("_split" + i, csm.splits[i]);
        }

        //Shader.SetGlobalTexture("_ShadowMap", shadowTex);
        Shader.SetGlobalTexture("_ShadowStrengthTex", shadowStrengthTex);

        ShadowCastingPass(context, camera);

        GBufferPass(context, camera);

        ShadowMappingPass(context, camera);

        LightPass(context, camera);

        //绘制天空盒
        context.DrawSkybox(camera);

        if (Handles.ShouldRenderGizmos())
        {
            context.DrawGizmos(camera, GizmoSubset.PreImageEffects);
            context.DrawGizmos(camera, GizmoSubset.PostImageEffects);
        }

        context.Submit();
    }


    void ShadowCastingPass(ScriptableRenderContext context, Camera camera)
    {
        //初始化相机视锥体
        shadowMapping.frustum.InitByCamera(camera, camera.nearClipPlane, shadowMapSettings.MaxDistance);
        Shader.SetGlobalFloat("_maxShadowDistance", shadowMapSettings.MaxDistance);
            
        //保存相机设置
        CameraSettings settings = new CameraSettings();
        settings.Save(ref camera);
        
        //初始化各层级视锥体
        for (var i = 0; i < 4; i++)
        {
            var dir = shadowMapping.frustum.farCorners[i] - shadowMapping.frustum.nearCorners[i];
            
            csm.subShadowMapping[0].frustum.nearCorners[i] = shadowMapping.frustum.nearCorners[i];
            csm.subShadowMapping[0].frustum.farCorners[i] = csm.subShadowMapping[0].frustum.nearCorners[i] + dir * csm.splits[0];

            csm.subShadowMapping[1].frustum.nearCorners[i] = csm.subShadowMapping[0].frustum.farCorners[i];
            csm.subShadowMapping[1].frustum.farCorners[i] = csm.subShadowMapping[1].frustum.nearCorners[i] + dir * csm.splits[1];
            
            csm.subShadowMapping[2].frustum.nearCorners[i] = csm.subShadowMapping[1].frustum.farCorners[i];
            csm.subShadowMapping[2].frustum.farCorners[i] = csm.subShadowMapping[2].frustum.nearCorners[i] + dir * csm.splits[2];
            
            csm.subShadowMapping[3].frustum.nearCorners[i] = csm.subShadowMapping[2].frustum.farCorners[i];
            csm.subShadowMapping[3].frustum.farCorners[i] = csm.subShadowMapping[3].frustum.nearCorners[i] + dir * csm.splits[3];
        }

        //依次渲染子视锥
        Light light = RenderSettings.sun;
        Vector3 lightDir = light.transform.rotation * Vector3.forward;
        for (var i = 0; i < 4; i++)
        {
            //配置阴影相机
            csm.subShadowMapping[i].ConfigShadowCamera(ref camera, lightDir, (int)shadowMapSettings.Resolution);
            //光源裁剪空间投影矩阵
            Matrix4x4 V = camera.worldToCameraMatrix;
            Matrix4x4 P = GL.GetGPUProjectionMatrix(camera.projectionMatrix, false);
            Shader.SetGlobalMatrix("_vpMatrixShadow" + i, P * V);

            //设置渲染目标为指定层级的阴影纹理
            CommandBuffer cmd = new CommandBuffer();
            cmd.name = "ShadowCastingLevel"+i;
            context.SetupCameraProperties(camera);
            cmd.SetRenderTarget(shadowTextures[i]);
            cmd.ClearRenderTarget(true, true, Color.clear);
            context.ExecuteCommandBuffer(cmd);
            cmd.Clear();

            //剔除
            camera.TryGetCullingParameters(out var cullingParameters);
            var cullingResults = context.Cull(ref cullingParameters);

            //渲染
            ShaderTagId shaderTagId = new ShaderTagId("DepthOnly");
            SortingSettings sortingSettings = new SortingSettings(camera);
            DrawingSettings drawingSettings = new DrawingSettings(shaderTagId, sortingSettings);
            FilteringSettings filteringSettings = FilteringSettings.defaultValue;
            context.DrawRenderers(cullingResults, ref drawingSettings, ref filteringSettings);

            context.Submit();
        }

        //恢复渲染相机设置
        settings.Revert(ref camera);
        context.SetupCameraProperties(camera);
    }

    void GBufferPass(ScriptableRenderContext context, Camera camera)
    {
        context.SetupCameraProperties(camera);
        CommandBuffer cmd = new CommandBuffer();
        cmd.name = "GBuffer";

        //清屏
        cmd.SetRenderTarget(gbufferID, gdepth);
        cmd.ClearRenderTarget(true, true, Color.black);
        context.ExecuteCommandBuffer(cmd);
        cmd.Clear();

        //剔除
        camera.TryGetCullingParameters(out var cullingParameters);
        var cullingResults = context.Cull(ref cullingParameters);

        //渲染设置
        ShaderTagId shaderTagId = new ShaderTagId("GBuffer");
        SortingSettings sortingSettings = new SortingSettings(camera);
        DrawingSettings drawingSettings = new DrawingSettings(shaderTagId, sortingSettings);
        FilteringSettings filteringSettings = FilteringSettings.defaultValue;

        //绘制物体
        context.DrawRenderers(cullingResults, ref drawingSettings, ref filteringSettings);

        context.Submit();
    }

    void ShadowMappingPass(ScriptableRenderContext context, Camera camera)
    {
        CommandBuffer cmd = new CommandBuffer();
        cmd.name = "ShadowMapping";

        Material mat = new Material(Shader.Find("TRP/ShaderMapping"));
        cmd.Blit(gbufferID[0], shadowStrengthTex, mat);
        context.ExecuteCommandBuffer(cmd);
        context.Submit();
    }

    void LightPass(ScriptableRenderContext context, Camera camera)
    {
        CommandBuffer cmd = new CommandBuffer();
        cmd.name = "LightPass";

        Material mat = new Material(Shader.Find("TRP/LightPass"));
        cmd.Blit(gbufferID[0], BuiltinRenderTextureType.CameraTarget, mat);
        context.ExecuteCommandBuffer(cmd);

        context.Submit();
    }
}