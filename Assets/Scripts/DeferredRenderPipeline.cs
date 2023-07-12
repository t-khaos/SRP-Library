using UnityEditor;
using UnityEngine;
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
    private RenderTexture shadowTex;
    private int shadowMapSize = 1024;
    private ShadowMapping _shadowMapping = new ShadowMapping();
    public ShadowMapSettings ShadowMapSettings;
    public float _orthoDistance = 500;

    public DeferredRenderPipeline()
    {
        gdepth = new RenderTexture(Screen.width, Screen.height, 24, RenderTextureFormat.Depth, RenderTextureReadWrite.Linear);
        gbuffers[0] = new RenderTexture(Screen.width, Screen.height, 0, RenderTextureFormat.ARGB32, RenderTextureReadWrite.sRGB);
        gbuffers[1] = new RenderTexture(Screen.width, Screen.height, 0, RenderTextureFormat.ARGB32, RenderTextureReadWrite.Linear);
        gbuffers[2] = new RenderTexture(Screen.width, Screen.height, 0, RenderTextureFormat.ARGB32, RenderTextureReadWrite.Linear);
        gbuffers[3] = new RenderTexture(Screen.width, Screen.height, 0, RenderTextureFormat.ARGB32, RenderTextureReadWrite.Linear);

        for (int i = 0; i < 4; i++)
            gbufferID[i] = gbuffers[i];


        shadowTex = new RenderTexture(shadowMapSize, shadowMapSize, 24, RenderTextureFormat.Depth, RenderTextureReadWrite.Linear);
    }
    protected override void Render(ScriptableRenderContext context, Camera[] cameras)
    {
        if (cameras.Length == 0) return;
        Camera camera = cameras[0];
        context.SetupCameraProperties(camera);

        //设置Shader全局变量

        //GBuffer纹理
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

        Shader.SetGlobalTexture("_DiffuseIBL", DiffuseIBL);
        Shader.SetGlobalTexture("_SpecularIBL", SpecularIBL);
        Shader.SetGlobalTexture("_BRDFLUT", BRDFLUT);
        
        
        Shader.SetGlobalTexture("_ShadowMap", shadowTex);


        GBufferPass(context, camera);

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
        Light light = RenderSettings.sun;
        Vector3 lightDir = light.transform.forward;

        //保存渲染相机设置，配置为阴影相机
        _shadowMapping.UpdateFrustem(camera, lightDir);
        _shadowMapping.SaveRenderingCameraSettings(ref camera);
        _shadowMapping.ConfigShadowCamera(ref camera, lightDir, _orthoDistance);
        
        //光源裁剪空间投影矩阵
        Matrix4x4 V = camera.worldToCameraMatrix;
        Matrix4x4 P = GL.GetGPUProjectionMatrix(camera.projectionMatrix, false);
        Shader.SetGlobalMatrix("_WorldToClipLight", P*V);

        CommandBuffer cmd = new CommandBuffer();
        cmd.name = "ShadowCasting";
        
        //更新渲染目标
        cmd.SetupCameraProperties(camera);
        cmd.SetRenderTarget(shadowTex);
        cmd.ClearRenderTarget(true,true,Color.clear);
        context.ExecuteCommandBuffer(cmd);
        cmd.Clear();

        //剔除
        if(!camera.TryGetCullingParameters(out var cullingParameters))  return;
        cullingParameters.shadowDistance = Mathf.Min(ShadowMapSettings.MaxDistance, camera.farClipPlane);
        var cullingResults = context.Cull(ref cullingParameters);
        
        //渲染
        ShaderTagId shaderTagId = new ShaderTagId("ShadowCasting");
        SortingSettings sortingSettings = new SortingSettings(camera);
        DrawingSettings drawingSettings = new DrawingSettings(shaderTagId, sortingSettings);
        FilteringSettings filteringSettings = FilteringSettings.defaultValue;
        context.DrawRenderers(cullingResults, ref drawingSettings, ref filteringSettings);
        
        context.Submit();
        
        //恢复渲染相机设置
        _shadowMapping.RevertRenderingCameraSettings(ref camera);
    }
    void GBufferPass(ScriptableRenderContext context, Camera camera)
    {
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
