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

//https://discussions.unity.com/t/shader-inverse-float4x4-function/36738
float4x4 inverse(float4x4 input)
{
    #define minor(a,b,c) determinant(float3x3(input.a, input.b, input.c))
    //determinant(float3x3(input._22_23_23, input._32_33_34, input._42_43_44))
	
    float4x4 cofactors = float4x4(
         minor(_22_23_24, _32_33_34, _42_43_44), 
        -minor(_21_23_24, _31_33_34, _41_43_44),
         minor(_21_22_24, _31_32_34, _41_42_44),
        -minor(_21_22_23, _31_32_33, _41_42_43),
		
        -minor(_12_13_14, _32_33_34, _42_43_44),
         minor(_11_13_14, _31_33_34, _41_43_44),
        -minor(_11_12_14, _31_32_34, _41_42_44),
         minor(_11_12_13, _31_32_33, _41_42_43),
		
         minor(_12_13_14, _22_23_24, _42_43_44),
        -minor(_11_13_14, _21_23_24, _41_43_44),
         minor(_11_12_14, _21_22_24, _41_42_44),
        -minor(_11_12_13, _21_22_23, _41_42_43),
		
        -minor(_12_13_14, _22_23_24, _32_33_34),
         minor(_11_13_14, _21_23_24, _31_33_34),
        -minor(_11_12_14, _21_22_24, _31_32_34),
         minor(_11_12_13, _21_22_23, _31_32_33)
    );
    #undef minor
    return transpose(cofactors) / determinant(input);
}