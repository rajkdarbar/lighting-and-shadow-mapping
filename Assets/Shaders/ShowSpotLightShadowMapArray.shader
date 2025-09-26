
Shader "Custom/ShowSpotLightShadowMapArray"
{
    Properties
    {
        _ShadowMaps("Shadow Maps", 2DArray) = "" {}
        _Slice("Slice Index", Int) = 0
    }
    SubShader
    {
        Tags { "Queue" = "Overlay" "RenderType" = "Opaque" }
        Pass
        {
            ZTest Always
            ZWrite Off
            Cull Off
            Lighting Off

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
                float4 pos : SV_POSITION;
                float2 uv : TEXCOORD0;
            };

            v2f vert(appdata v)
            {
                v2f o;
                o.pos = UnityObjectToClipPos(v.vertex);
                o.uv = v.uv;
                return o;
            }

            // Declare array
            UNITY_DECLARE_TEX2DARRAY(_ShadowMaps);
            int _Slice;

            fixed4 frag(v2f i) : SV_Target
            {
                // Sample depth from given slice
                float raw = UNITY_SAMPLE_TEX2DARRAY(_ShadowMaps, float3(i.uv, _Slice)).r;

                // Convert to linear [0..1]
                float d = Linear01Depth(raw);

                // Flip if reversed Z (Unity uses this on DX11 +)
                #if defined(UNITY_REVERSED_Z)
                d = 1.0 - d;
                #endif

                return fixed4(d, d, d, 1);
            }
            ENDCG
        }
    }
}


