///[-1,1]to[0,1]
half4 UnitVectorToColor(half3 N)
{
    return half4(N * 0.5 + 0.5,1);
}

//[0,1]to[-1,1]
half3 ColorToUnitVector(half4 Color)
{
    return Color.xyz * 2 - 1;
}

half2 UnitVectorToSpherical(half3 N)
{
    half2 enc = normalize(N.xy) * (sqrt(-N.z*0.5+0.5));
    enc = enc*0.5+0.5;
    return enc;
}

half3 SphericalToUnitVector(half4 Color)
{
    half4 N = Color*half4(2,2,0,0) + half4(-1,-1,1,-1);
    half l = dot(N.xyz,-N.xyw);
    N.z = l;
    N.xy *= sqrt(l);
    return N.xyz * 2 + half3(0,0,-1);
}

half2 UnitVectorToOctahedron(half3 N)
{
    N.xy /= dot( 1, abs(N) );
    if( N.z <= 0 )
    {
        N.xy = ( 1 - abs(N.yx) ) * ( N.xy >= 0 ? half2(1,1) : half2(-1,-1) );
    }
    return N.xy*0.5+0.5;
}

half3 OctahedronToUnitVector(half2 Oct)
{
    Oct = Oct * 2 - 1;
    half3 N = half3( Oct, 1 - dot( 1, abs(Oct) ) );
    if( N.z < 0 )N.xy = ( 1 - abs(N.yx) ) * ( N.xy >= 0 ? half2(1,1) : half2(-1,-1) );
    return normalize(N);
}

half3 ACESFilm(half3 x)
{
    half a = 2.51f;
    half b = 0.03f;
    half c = 2.43f;
    half d = 0.59f;
    half e = 0.14f;
    return saturate((x * (a * x + b)) / (x * (c * x + d) + e));
}

half DecodeRGBA2half(half4 rgba) {
    const half4 bitShift = half4(1.0, 1.0/256.0, 1.0/(256.0*256.0), 1.0/(256.0*256.0*256.0));
    return dot(rgba, bitShift);
}

half4 Encodehalf2RGBA(half depth) {
    const half4 bitShift = half4(1.0, 256.0, 256.0 * 256.0, 256.0 * 256.0 * 256.0);
    const half4 bitMask = half4(1.0/256.0, 1.0/256.0, 1.0/256.0, 0.0);
    half4 rgbaDepth = frac(depth * bitShift);
    rgbaDepth -= rgbaDepth.gbaa * bitMask;
    return rgbaDepth;
}

half ShadowMap01(sampler2D _ShadowMap, half3 ShadowCoord)
{
    half d_frag = ShadowCoord.z;
    half d_shadow = DecodeRGBA2half(tex2D(_ShadowMap, ShadowCoord.xy));
#if defined (UNITY_REVERSED_Z)
    if(d_shadow>d_frag) return 0.0f;
#else
    if(d_shadow<d_frag) return 0.0f;
#endif

    return 1.0;
}