Shader "Retro Image Effect Shaders/Fast CRT simple"
{
    Properties
    {
        _MainTex ("Texture", 2D) = "white" {}
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
        Pass
        {
            Name "CRTScanline"
            
            Blend One Zero

            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag

            #include "UnityCG.cginc"

            struct appdata
            {
                float4 vertex : POSITION; 
                float2 uv : TEXCOORD0;
            };

            struct v2f
            {
                //half4 color : COLOR0;
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

            sampler2D _MainTex;
            sampler2D _CRTScanlineTexture;
            float _CRTScanlineIntensity;
            float _CRTScanlineBloom;
            float _ResolutionX;
            float _ResolutionY;

            half4 hardLight(half4 srcColor, half4 overlayColor)
            {
                half4 result = srcColor;
                result.rgb = (overlayColor.rgb < 0.5) ? (2.0 * srcColor.rgb * overlayColor.rgb) : (1.0 - 2.0 * (1.0 - srcColor.rgb) * (1.0 - overlayColor.rgb));
                return result;
            }

            half4 multiply(half4 srcColor, half4 overlayColor)
            {
                half4 result = srcColor * overlayColor;
                result.a = srcColor.a;
                return result;
            }

            half4 frag (v2f i) : SV_Target
            {
                half4 srcCol = tex2D(
                    _MainTex,
                    i.uv
                );
                half4 scanlineCol =  tex2D(
                    _CRTScanlineTexture,
                    float2(
                        i.uv.x * _ResolutionX,
                        i.uv.y * _ResolutionY
                    )
                );

                // "bloom" formula
                // the weights below are rough approximation of each color
                // channel's contribution to perceptual brightness;
                // green is the brightest color, followed by red and then blue
                half srcColValue = clamp(
                    (srcCol.r * 3 + srcCol.g * 4 + srcCol.b * 2) / 9.0 - 0.5,
                    0,
                    0.5
                );

                // multiplying by the scanline texture divides the color brightness in 3,
                // so we multiply it by 3 here to compensate
                half4 scanlinedCol = multiply(srcCol, scanlineCol * 1.5) * 3 * 1.5;
                half4 outCol = lerp(srcCol, scanlinedCol, _CRTScanlineIntensity);
                outCol.rgb = lerp(outCol.rgb, half3(1, 1, 1), srcColValue * _CRTScanlineBloom);
                outCol = saturate(outCol); // clamp
                return outCol;
            }
            ENDCG
        }
    }
}
