Shader "TRP/GBuffer"
{
    Properties
    {
        [Toggle] _UseBaseColorTex("Use Albedo Tex", Float) = 1
        _BaseColor("BaseColor", color) = (1,1,1,1)
        _BaseColorTex("BaseColorTex", 2D) = "white" {}

        [Toggle] _UseMetallicTex("Use Metallic Tex", Float) = 1
        _Metallic("Metallic", Range(0, 1)) = 0.5
        _MetallicTex("MetallicTex", 2D) = "white" {}

        [Toggle] _UseRoughnessTex("Use Roughness Tex", Float) = 1
        _Roughness("Roughness", Range(0, 1)) = 0.5
        _RoughnessTex("RoughnessTex", 2D) = "white" {}

        [Toggle] _UseNormalTex("Use Normal Tex", Float) = 1
        _NormalTex("NormalTex", 2D) = "bump" {}

        [Toggle] _UseAOTex("Use AO Tex", Float) = 1
        _AOTex("AOTex", 2D) = "white" {}

        [Toggle] _UseEmissionTex("Use Emission Tex", Float) = 1
        _Emission("Emission", color) = (1,1,1,1)
        _EmissionTex("EmissionTex", 2D) = "black" {}
    }
    SubShader
    {
        Tags { "LightMode"="GBuffer" }
        LOD 100

        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag

            #include "UnityCG.cginc"
            #include "Math.cginc"

            struct appdata
            {
                half4 vertex : POSITION;
                half2 uv : TEXCOORD0;
                half4 tangent : TANGENT;
                half3 normal : NORMAL;
            };

            struct v2f
            {
                half4 vertex : SV_POSITION;
                half2 uv : TEXCOORD0;
                half3 tangent : TANGENT;
                half3 normal : NORMAL;
            };

            struct GBufferOutput{
                half4 GT0 : SV_TARGET0;
                half4 GT1 : SV_TARGET1;
                half4 GT2 : SV_TARGET2;
                half4 GT3 : SV_TARGET3;
            };

            sampler2D _MainTex;
            float4 _MainTex_ST;

            sampler2D _BaseColorTex, _MetallicTex, _RoughnessTex;
            sampler2D _EmissionTex, _NormalTex, _AOTex;

            half _UseBaseColorTex, _UseMetallicTex, _UseRoughnessTex;
            half _UseEmissionTex, _UseNormalTex, _UseAOTex;
            half _Metallic, _Roughness;
            half3 _BaseColor, _Emission;

            v2f vert (appdata v)
            {
                v2f o;
                o.vertex = UnityObjectToClipPos(v.vertex);
                o.uv = v.uv;
                o.normal = UnityObjectToWorldNormal(v.normal);
                o.tangent = UnityObjectToWorldDir(v.tangent)*v.tangent.w;
                return o;
            }

            GBufferOutput frag(v2f i)
            {
                GBufferOutput o;
                half2 uv = i.uv;

                //构建TBN矩阵
                half3 T = normalize(i.tangent);
                half3 N = i.normal;
                half3 B = normalize(cross(N,T));

                //若使用法线贴图则解包法线贴图
                if (_UseNormalTex) {
                    half3 Normal = UnpackNormal(tex2D(_NormalTex, uv));
                    half3x3 TBN = half3x3(T, B, N);
                    N = normalize(mul(Normal, TBN));
                }

                //读取贴图
                half3 BaseColor = tex2D(_BaseColorTex, uv).rgb;
                half Metallic = tex2D(_MetallicTex, uv).r;
                half3 Emission = tex2D(_EmissionTex, uv).rgb;
                half Roughness = tex2D(_RoughnessTex, uv).r;
                half AO = tex2D(_AOTex, uv).r;

                if (!_UseBaseColorTex) BaseColor = _BaseColor;
                if (!_UseMetallicTex) Metallic = _Metallic;
                if (!_UseRoughnessTex) Roughness = _Roughness;
                if (!_UseAOTex) AO = 1;
                if (!_UseEmissionTex) Emission = _Emission;

                //编码GBuffer
                o.GT0 = half4(BaseColor, Metallic);
                o.GT1 = UnitVectorToColor(N);
                o.GT2 = half4(Emission,Roughness);
                o.GT3 = half4(AO,0,0,1);
                return o;
            }

            ENDCG
        }
    }
}
