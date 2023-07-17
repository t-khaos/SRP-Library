Shader "TRP/LightPass"
{
    Properties
    {
    }
    SubShader
    {
        Cull Off ZWrite On ZTest Always


        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag

            #include "UnityCG.cginc"
            #include "BRDF.cginc"
            #include "Common.cginc"

            struct appdata
            {
                half4 vertex : POSITION;
                half2 uv : TEXCOORD0;
            };

            struct v2f
            {
                half2 uv : TEXCOORD0;
                half4 vertex : SV_POSITION;
            };

            v2f vert (appdata v)
            {
                v2f o;
                o.vertex = UnityObjectToClipPos(v.vertex);
                o.uv = v.uv;
                return o;
            }

            sampler2D _gdepth;
            sampler2D _GT0;
            sampler2D _GT1;
            sampler2D _GT2;
            sampler2D _GT3;

            samplerCUBE _DiffuseIBL;
            samplerCUBE _SpecularIBL;
            sampler2D _BRDFLUT;
            
            sampler2D _ShadowStrengthTex;

            half4x4 _vpMatrix;
            half4x4 _vpMatrixInv;

            fixed4 frag (v2f i, out half depth:SV_Depth) : SV_Target
            {
                half2 uv = i.uv;

                //解码GBuffer
                half3 BaseColor = tex2D(_GT0,uv).rgb;
                half Metallic = tex2D(_GT0,uv).a;

                half3 Normal = ColorToUnitVector(tex2D(_GT1,uv));
                half3 Emission = tex2D(_GT2,uv).rgb;
                half Roughness = tex2D(_GT2,uv).a;
                half AO = tex2D(_GT3,uv).r;

                //根据屏幕空间坐标uv[0,1]反推世界空间坐标
                half d = UNITY_SAMPLE_DEPTH(tex2D(_gdepth, uv));
                depth = d;

                half4 ndcPos = half4(uv*2-1,d,1);
                half4 worldPos = mul(_vpMatrixInv, ndcPos);
                worldPos /= worldPos.w;
 
                //计算PBR光照所需变量
                half3 N = normalize(Normal);
                half3 L = normalize(UnityWorldSpaceLightDir(worldPos.xyz));
                half3 V = normalize(UnityWorldSpaceViewDir(worldPos.xyz));

                half3 Radiance = half3(PI,PI,PI);

                half3 Direct = PBR(N, V, L, BaseColor, Radiance ,Roughness, Metallic); 
                //half3 Indirect = 0; 
                half3 Indirect = IBL(
                    N, V, BaseColor, Roughness, Metallic,
                    _DiffuseIBL, _SpecularIBL, _BRDFLUT
                );

                half visibility = tex2D(_ShadowStrengthTex, uv).r;

                half3 color = Direct* visibility + Indirect*AO + Emission;
                //half3 color = Direct + Indirect*AO;
                //Reinhard 只压缩高亮度
                //color = color / (color + 1.0);
                color = ACESFilm(color);

                return half4(color ,1);
            }
            ENDCG
        }
    }
}
