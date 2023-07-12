using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Serialization;

[CreateAssetMenu(menuName = "Rendering/Deferred Render Pipeline")]

public class DeferredRenderPipelineAsset : RenderPipelineAsset
{
    public Cubemap DiffuseIBL;
    public Cubemap SpecularIBL;
    public Texture BRDFLUT;

    [SerializeField] public ShadowMapSettings ShadowMapSettings = default;
    protected override RenderPipeline CreatePipeline()
    {
        var RP = new DeferredRenderPipeline();

        RP.DiffuseIBL = DiffuseIBL;
        RP.SpecularIBL = SpecularIBL;   
        RP.BRDFLUT = BRDFLUT;
        RP.ShadowMapSettings = ShadowMapSettings;
        
        return RP;
    }

}
