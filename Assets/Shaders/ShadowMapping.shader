Shader "TRP/ShaderMapping"
{
    Properties {}
    SubShader
    {
        Cull off ZWrite off ZTest Always
        Tags
        {
            "LightMode"="ShaderMapping"
        }
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

            v2f vert(appdata v)
            {
                v2f o;
                o.vertex = UnityObjectToClipPos(v.vertex);
                o.uv = v.uv;
                return o;
            }

            fixed4 frag(v2f i) : SV_Target
            {
                float2 uv = i.uv; 
                float3 normal = tex2D(_GT1, uv).rgb * 2 - 1;
                float d = UNITY_SAMPLE_DEPTH(tex2D(_gdepth, uv));
                //float d_lin = Linear01Depth(d);

                // 反投影重建世界坐标
                float4 ndcPos = float4(uv*2-1, d, 1);
                float4 worldPos = mul(_vpMatrixInv, ndcPos);
                worldPos /= worldPos.w;

                float3 lightDir = normalize(_WorldSpaceLightPos0.xyz);
                float bias = max(0.001 * (1.0 - dot(normal, lightDir)), 0.001);
                if(dot(lightDir, normal) < 0.005) return 0;
                float shadow = 1.0;
                worldPos.xyz += normal * 0.03;
                shadow *= ShadowMap01(worldPos, _ShadowMap, _vpMatrixShadow);
                return shadow;
            }
            ENDCG
        }
    }
}