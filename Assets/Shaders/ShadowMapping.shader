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
            #include "Shadow.cginc"

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
                float d = tex2D(_gdepth, uv).r;

                //重建片元世界坐标
                float4 ndcPos = float4(uv*2-1, d, 1);
                float4 worldPos = mul(_vpMatrixInv, ndcPos);
                worldPos /= worldPos.w;

                //片元深度沿着法线方向偏移
                float3 lightDir = normalize(_WorldSpaceLightPos0.xyz);
                float3 normal = tex2D(_GT1, uv).rgb * 2 - 1;
                if(dot(lightDir, normal) < 0.001) return 0;
                worldPos.xyz += normal * 0.03;

                //片元坐标投影到光源空间
                float4 shadowCoord = mul(_vpMatrixShadow, worldPos);
                shadowCoord /= shadowCoord.w;
                shadowCoord.xy = shadowCoord.xy * 0.5 + 0.5;
                        
                //采样阴影贴图
                float shadow = 1.0;
                shadow *= ShadowMap(_ShadowMap, shadowCoord.xyz);
                return shadow;
            }
            ENDCG
        }
    }
}