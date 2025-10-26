
Shader "Custom/ShadowCasterPointLight"
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

            float3 _PointLightPos;
            float _PointLightRange;
            float4x4 _PointLightViewProjectionMatrix;

            struct appdata
            {
                float4 vertex : POSITION;
            };

            struct v2f
            {
                float4 pos : SV_POSITION;
                float3 worldPos : TEXCOORD0;
            };

            v2f vert(appdata v)
            {
                v2f o;
                float4 worldPos = mul(unity_ObjectToWorld, v.vertex);
                o.pos = mul(_PointLightViewProjectionMatrix, worldPos);
                o.worldPos = worldPos.xyz;

                return o;
            }

            float frag(v2f i) : SV_Target
            {
                float dist = length(i.worldPos - _PointLightPos) / _PointLightRange;
                return saturate(dist);
            }
            ENDCG
        }
    }
}