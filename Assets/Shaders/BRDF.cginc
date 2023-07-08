#define PI 3.14159265359

half DistributionGGX(half NoH, half Roughness)
{
    half a = Roughness * Roughness;
    half a2 = a * a;
    half NoH2 = NoH * NoH;
    
    half nom = a2;
    half denom = NoH2 * (a2 - 1.0) + 1.0;
    denom = PI * denom * denom;
    
    return nom / denom;
}

half3 FresnelSchlick(half u, half3 f0)
{
    return f0 + (1.0 - f0) * pow(clamp(1.0 - u, 0.0 ,1.0), 5.0);
}

half GeometrySchlickGGX(half u, half Roughness)
{
    half r = Roughness + 1.0;
    half k = (r * r) / 8.0;
    
    half nom = u;
    half denom = u * (1.0 - k) + k;
    
    return nom / denom;
}

float GeometrySmith(half NoV, half NoL, half Roughness)
{
    half GGXV = GeometrySchlickGGX(NoV, Roughness);
    half GGXL = GeometrySchlickGGX(NoL, Roughness);
    return GGXV * GGXL;
}

half3 PBR(half3 N, half3 V, half3 L, half3 BaseColor, half3 Radiance, half Roughness, half Metallic)
{
    half3 f0 = lerp(0.04, BaseColor, Metallic);

    half3 H = normalize(L + V);

    half NoV = clamp(dot(N, L), 0.0, 1.0);
    half NoL = clamp(dot(N, L), 0.0, 1.0);
    half NoH = clamp(dot(N, H), 0.0, 1.0);
    half LoH = clamp(dot(L, H), 0.0, 1.0);

    //线性粗糙度重映射
    half D = DistributionGGX(NoH, Roughness);
    half3 F = FresnelSchlick(LoH, f0);
    half G = GeometrySmith(NoV, NoL, Roughness);

    half3 Ks = F;
    half3 Kd = 1.0 - F;
    Kd *= 1.0 - Metallic;

    half3 Specular = D * G * F / (4.0 * max(NoV, 0.0) * max(NoL,0.0) + 0.0001);
    half3 Difusse = Kd * BaseColor / PI;
    
    half3 Direct =  (Difusse + Specular) * NoL * Radiance;
    return Direct;
}

half3 FresnelSchlickRoughness(half NoV, half3 f0, half Roughness)
{
    half r1 = 1.0f - Roughness;
    return f0 + (max(half3(r1, r1, r1), f0) - f0) * pow(1 - NoV, 5.0f);
}

half3 IBL(
    half3 N, half3 V, half3 BaseColor, half Roughness, half Metallic,
    samplerCUBE _DiffuseIBL, samplerCUBE _SpecularIBL, sampler2D _BRDFLUT)
{

    Roughness = min(Roughness, 0.99); //防止NoV和Roughness都为1有bug

    half3 f0 = lerp(0.04, BaseColor, Metallic);

    half3 H = normalize(N);

    half NoV = clamp(dot(N, V), 0.0, 1.0);
    half HoV = clamp(dot(H, V), 0.0, 1.0);
    
    half3 R = normalize(reflect(-V, N));

    half3 F = FresnelSchlickRoughness(NoV, f0, Roughness);

    half3 Ks = F;
    half3 Kd = 1.0 - F;
    Kd *= 1.0 - Metallic;


    half3 IBLd = texCUBE(_DiffuseIBL, N).rgb;

    //Unity中的快速计算
    Roughness = Roughness * (1.7 - 0.7 * Roughness);
    //得到Specular IBL的MipMap LOD层级
    half mip = Roughness * 6.0;

    half3 IBLs = texCUBElod(_SpecularIBL, half4(R, mip)).rgb;
    half2 BRDF = tex2D(_BRDFLUT, half2(NoV, Roughness)).rg;


    half3 Diffuse = Kd * IBLd * BaseColor;
    //渲染方程将菲涅尔项提出后可得两个积分：F0*INT1+INT2
    half3 Specular = IBLs * (f0 * BRDF.x + BRDF.y);
    
    float3 Indirect = Diffuse + Specular;
    return Indirect;
}


