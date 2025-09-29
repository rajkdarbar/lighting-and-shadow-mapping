
Shader "Custom/ShadowCasterPointLight"
{
    SubShader
    {
        Tags { "RenderType" = "Opaque" }
        Pass
        {
            Cull Front
            Offset 2, 2
            ZWrite On

            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "UnityCG.cginc"

            struct appdata
            {
                float4 vertex : POSITION;
            };

            struct v2f {
                float4 pos : SV_POSITION;
                float3 worldPos : TEXCOORD0;
            };

            uniform float3 _PointLightPos0;
            uniform float _PointLightRange0;

            v2f vert(appdata v) {
                v2f o;
                o.pos = UnityObjectToClipPos(v.vertex);
                o.worldPos = mul(unity_ObjectToWorld, v.vertex).xyz;
                return o;
            }

            float frag(v2f i) : SV_Target {
                float dist = length(i.worldPos - _PointLightPos0) / _PointLightRange0;
                return dist;
            }
            ENDCG
        }
    }
}