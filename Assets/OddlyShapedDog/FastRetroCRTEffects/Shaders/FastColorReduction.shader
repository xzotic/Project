Shader "Retro Image Effect Shaders/Fast Color Reduction"
{
    Properties
    {
        _MainTex ("Texture", 2D) = "white" {}
        _ColorDepth ("Color depth", Range(1,64)) = 8
    }
    SubShader
    {
        // No culling or depth
        Cull Off ZWrite Off ZTest Always

        // color reduction pass
        Pass
        {
            Name "ColorReduction"

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
            
            sampler2D _MainTex;
            float _ColorDepth;

            half4 frag (v2f i) : SV_Target
            {
                half4 col = tex2D(_MainTex, i.uv);

                // reduce the color precision
                float numColors = ceil(pow(2, _ColorDepth) / 3);
                col.rgb = floor(col.rgb * numColors) / numColors;

                return col;
            }
            ENDCG
        }
    }
}
