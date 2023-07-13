float ShadowMap(sampler2D _ShadowTex, float3 shadowCoord)
{
    float cameraDepth = shadowCoord.z;
    float smapleDepth = tex2D(_ShadowTex, shadowCoord.xy).r;
    
    #if defined (UNITY_REVERSED_Z)
    if(smapleDepth>cameraDepth) return 0.0f;
    #else
    if(smapleDepth<cameraDepth) return 0.0f;
    #endif

    return 1.0f;
}