Shader "TRP/ShaderMapping"
{
    Properties
    {

    }
    SubShader
    {
        Cull off ZWrite off ZTest Always
        Tags { "LightMode"="ShaderMapping"}
        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag

            #include "UnityCG.cginc"
            #include "Common.cginc"

            //VP矩阵
            float4x4 _vpMatrix;
            float4x4 _vpMatrixInv;
            //平行光正交投影矩阵
            float4x4 _vpMatrixShadow;
            //GBuffer
            sampler2D _gdepth, _GT1;
            sampler2D _ShadowMap;

            

            struct appdata
            {
                float4 vertex : POSITION;
                float2 uv : TEXCOORD0;
            };

            struct v2f
            {
                float2 uv : TEXCOORD0;
                float4 vertex : SV_POSITION;
            };

            v2f vert (appdata v)
            {
                v2f o;
                o.vertex = UnityObjectToClipPos(v.vertex);
                o.uv = v.uv;
                return o;
            }

            float frag (v2f i) : SV_Target
            {
                float2 uv = i.uv;

                float d = UNITY_SAMPLE_DEPTH(tex2D(_gdepth, uv));
                float linearDepth = Linear01Depth(d);

                float4 ndcPos = float4(uv*2-1, d, 1);
                float4 worldPos = mul(_vpMatrixInv, ndcPos);
                worldPos/=worldPos.w;

                float4 ndcShadowPos =  mul(_vpMatrixShadow, worldPos);
                ndcShadowPos /= ndcShadowPos.w;
                float3 ShadowCoord = ndcShadowPos.xyz;
                ShadowCoord.xy = ShadowCoord.xy *0.5+0.5;

                float visibility = ShadowMap01(_ShadowMap, ShadowCoord);

                return visibility;
            }
            ENDCG
        }
    }
}
