Shader "Custom/ShadowCasterSpotLight"
{
    SubShader
    {
        Tags { "RenderType" = "Opaque" }
        Pass
        {
            Cull Front
            ZWrite On
            ColorMask R // write to the R channel (RFloat texture)

            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "UnityCG.cginc"
            
            float4x4 _SpotLight_VP_ShadowCaster; // for rasterization
            float4x4 _SpotLight_View_ShadowCaster; // for view - space position
            float _SpotLightFar_ShadowCaster; // for normalization

            struct appdata
            {
                float4 vertex : POSITION;
            };

            struct v2f
            {
                float4 pos : SV_POSITION;
                float3 viewPos : TEXCOORD0;
            };

            v2f vert(appdata v)
            {
                v2f o;
                float4 worldPos = mul(unity_ObjectToWorld, v.vertex);
                o.pos = mul(_SpotLight_VP_ShadowCaster, worldPos);
                o.viewPos = mul(_SpotLight_View_ShadowCaster, worldPos).xyz;

                return o;
            }


            float frag(v2f i) : SV_Target
            {
                float linearDepth = length(i.viewPos); // distance from light in view - space
                linearDepth /= _SpotLightFar_ShadowCaster; // normalize to [0, 1] for easy debugging

                return linearDepth;
            }

            ENDCG
        }
    }
}
