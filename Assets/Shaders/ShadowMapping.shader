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
            float4x4 _vpMatrixShadow0, _vpMatrixShadow1, _vpMatrixShadow2, _vpMatrixShadow3;
            //GBuffer
            sampler2D _gdepth, _GT1;


            //sampler2D _ShadowMap;

            //CSM
            sampler2D _ShadowMap0, _ShadowMap1, _ShadowMap2, _ShadowMap3;
            float _split0, _split1, _split2, _split3;

            float _maxShadowDistance;

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
                float d = UNITY_SAMPLE_DEPTH(tex2D(_gdepth, uv));
                float d_lin = LinearEyeDepth(d);
                
                //重建片元世界坐标
                float4 ndcPos = float4(uv * 2 - 1, d, 1);
                float4 worldPos = mul(_vpMatrixInv, ndcPos);
                worldPos /= worldPos.w;

                //片元深度沿着法线方向偏移
                float3 lightDir = normalize(_WorldSpaceLightPos0.xyz);
                float3 normal = tex2D(_GT1, uv).rgb * 2 - 1;
                worldPos.xyz += normal * 0.001;
                
                if (dot(lightDir, normal) < 0.001) return 0;
                
                float shadow = 1.0;

                if (d_lin < _split0*_maxShadowDistance)
                {
                    shadow *= ShadowMap(_ShadowMap0, _vpMatrixShadow0, worldPos);
                }
                else if (d_lin < (_split0 + _split1)*_maxShadowDistance)
                {
                    shadow *= ShadowMap(_ShadowMap1, _vpMatrixShadow1, worldPos);
                }
                else if (d_lin < (_split0 + _split1 + _split2)*_maxShadowDistance)
                {
                    shadow *= ShadowMap(_ShadowMap2, _vpMatrixShadow2, worldPos);
                }
                else if (d_lin < (_split0 + _split1 + _split2 + _split3)*_maxShadowDistance)
                {
                    shadow *= ShadowMap(_ShadowMap3, _vpMatrixShadow3, worldPos);
                }
                return shadow;
            }
            ENDCG
        }
    }
}