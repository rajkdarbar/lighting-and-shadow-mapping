
Shader "Custom/ShadowCasterDirLight"
{
    SubShader
    {
        Tags { "RenderType" = "Opaque" }
        Pass
        {
            Cull Front // helps to reduce shadow acne
            ZWrite On

            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "UnityCG.cginc"

            float4x4 _DirLightViewProjectionMatrix;

            struct appdata
            {
                float4 vertex : POSITION;
            };

            struct v2f
            {
                float4 pos : SV_POSITION;
            };

            v2f vert(appdata v)
            {
                v2f o;
                float4 worldPos = mul(unity_ObjectToWorld, v.vertex);
                o.pos = mul(_DirLightViewProjectionMatrix, worldPos);

                return o;
            }

            float frag(v2f i) : SV_Target
            {
                float d = i.pos.z / i.pos.w; // d is in [0, 1]              
                return d;
            }

            ENDCG
        }
    }
}
