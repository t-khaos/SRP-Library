using UnityEngine;
using UnityEngine.Rendering;

[CreateAssetMenu(menuName = "Rendering/Deferred Render Pipeline")]

public class DeferredRenderPipelineAsset : RenderPipelineAsset
{
    public Cubemap DiffuseIBL;
    public Cubemap SpecularIBL;
    public Texture BRDFLUT;
    protected override RenderPipeline CreatePipeline()
    {
        var RP = new DeferredRenderPipeline();

        RP.DiffuseIBL = DiffuseIBL;
        RP.SpecularIBL = SpecularIBL;   
        RP.BRDFLUT = BRDFLUT;

        return RP;
    }

}
