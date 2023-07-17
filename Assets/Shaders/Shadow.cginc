


float ShadowMap(sampler2D _ShadowTex, float4x4 _vpMatrixShadow, float4 worldPos)
{
    //片元坐标投影到光源空间
    float4 shadowCoord = mul(_vpMatrixShadow, worldPos);
    shadowCoord /= shadowCoord.w;
    float2 uv = shadowCoord.xy * 0.5 + 0.5;

    if(uv.x<0 || uv.x>1 || uv.y<0 || uv.y>1) return 1.0f;
    
    float cameraDepth = shadowCoord.z;
    float sampleDepth = DecodeFloatRGBA(tex2D(_ShadowTex, uv));
    
    #if defined (UNITY_REVERSED_Z)
    if(sampleDepth>cameraDepth) return 0.0f;
    #else
    if(smapleDepth<cameraDepth) return 0.0f;
    #endif

    return 1.0f;
}

