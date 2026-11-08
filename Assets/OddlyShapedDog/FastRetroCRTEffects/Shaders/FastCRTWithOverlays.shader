Shader "Retro Image Effect Shaders/Fast CRT with overlays"
{
    Properties
    {
        _MainTex ("Texture", 2D) = "white" {}
        _CRTReflection ("CRT reflection", 2D) = "black" {}
        _CRTReflectionIntensity ("CRT reflection intensity", Range(0.0, 16.0)) = 1.0
        _CRTBorder ("CRT border", 2D) = "black" {}
        _CRTScanlineTexture ("CRT scanline texture", 2D) = "black" {}
        _CRTScanlineIntensity ("CRT scanline intensity", Range(0.0, 4.0)) = 1.0
        _CRTScanlineBloom ("CRT scanline bloom", Range(0.0, 4.0)) = 1.0
        _ColorDepth ("Color depth", Range(1,32)) = 8
        _ResolutionX ("Resolution X", Integer) = 320
        _ResolutionY ("Resolution Y", Integer) = 240
    }
    SubShader
    {
        // No culling or depth
        Cull Off ZWrite Off ZTest Always

        // color reduction pass
        UsePass "Retro Image Effect Shaders/Fast Color Reduction/ColorReduction"
        
        // CRT scanline pass
        UsePass "Retro Image Effect Shaders/Fast CRT simple/CRTScanline"

        // CRT reflection pass
        Pass
        {
            Name "CRTReflection"

            Blend OneMinusDstColor One

            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag

            #include "UnityCG.cginc"
            #include "FastImageEffectShadersCommon.cginc"

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
            
            sampler2D _CRTReflection;
            float _CRTReflectionIntensity;
            float4 _MainTex_ST;
            float _ResolutionX;
            float _ResolutionY;

            half4 frag (v2f i) : SV_Target
            {
                float desiredAspectRatio = _ResolutionX / _ResolutionY;
                float actualAspectRatio = _MainTex_ST.x / _MainTex_ST.y;

                float4 viewportRect = getAspectRatiodRect(
                    desiredAspectRatio,
                    actualAspectRatio
                );

                half4 reflectionCol = tex2D(
                    _CRTReflection,
                    //mapUVToRect(viewportRect, i.uv)
                    i.uv
                ).a * _CRTReflectionIntensity;
                return reflectionCol;
            }
            ENDCG
        }
        
        // CRT border pass
        Pass {
            Name "CRTBorder"

            Blend One OneMinusSrcAlpha

            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag

            #include "UnityCG.cginc"
            #include "FastImageEffectShadersCommon.cginc"

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
            
            sampler2D _CRTBorder;
            float4 _MainTex_TexelSize;
            float _ResolutionX;
            float _ResolutionY;

            half4 frag (v2f i) : SV_Target
            {
                float desiredAspectRatio = _ResolutionX / _ResolutionY;
                float actualAspectRatio = _MainTex_TexelSize.z / _MainTex_TexelSize.w;

                float4 viewportRect = getAspectRatiodRect(
                    desiredAspectRatio,
                    actualAspectRatio
                );

                half4 borderCol = tex2D(
                    _CRTBorder,
                    i.uv
                );
                return borderCol;
            }
            ENDCG
        }
    }
}
