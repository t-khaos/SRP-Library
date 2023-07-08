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
        N.xy = ( 1 - abs(N.yx) ) * ( N.xy >= 0 ? float2(1,1) : float2(-1,-1) );
    }
    return N.xy*0.5+0.5;
}

half3 OctahedronToUnitVector(half2 Oct)
{
    Oct = Oct * 2 - 1;
    float3 N = float3( Oct, 1 - dot( 1, abs(Oct) ) );
    if( N.z < 0 )N.xy = ( 1 - abs(N.yx) ) * ( N.xy >= 0 ? float2(1,1) : float2(-1,-1) );
    return normalize(N);
}

half3 ACESFilm(half3 x)
{
    float a = 2.51f;
    float b = 0.03f;
    float c = 2.43f;
    float d = 0.59f;
    float e = 0.14f;
    return saturate((x * (a * x + b)) / (x * (c * x + d) + e));
}