Shader "Hidden/RetroDither"
{
    Properties
    {
        _Levels ("Levels per channel", Float) = 16
        _DitherStrength ("Dither Strength", Range(0, 2)) = 1
    }
    SubShader
    {
        Tags { "RenderPipeline" = "UniversalPipeline" }
        ZWrite Off ZTest Always Cull Off

        Pass
        {
            Name "RetroDither"
            HLSLPROGRAM
            #pragma vertex Vert
            #pragma fragment Frag
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Packages/com.unity.render-pipelines.core/Runtime/Utilities/Blit.hlsl"

            float _Levels;
            float _DitherStrength;

            float Bayer8(uint2 p)
            {
                uint x = p.x & 7u;
                uint y = p.y & 7u;
                uint a = x ^ y;
                uint v = ((a & 1u) << 5) | ((y & 1u) << 4)
                       | ((a & 2u) << 2) | ((y & 2u) << 1)
                       | ((a & 4u) >> 1) | ((y & 4u) >> 2);
                return (v + 0.5) / 64.0;
            }

            half4 Frag(Varyings i) : SV_Target
            {
                float3 col = SAMPLE_TEXTURE2D_X(_BlitTexture, sampler_PointClamp, i.texcoord).rgb;
                col = LinearToSRGB(col);                       // work in perceptual space

                float steps = max(_Levels - 1.0, 1.0);
                float d = (Bayer8((uint2)i.positionCS.xy) - 0.5) * _DitherStrength / steps;
                col = floor((col + d) * steps + 0.5) / steps;

                col = SRGBToLinear(saturate(col));             // back to what the pipeline expects
                return half4(col, 1);
            }
            ENDHLSL
        }
    }
}