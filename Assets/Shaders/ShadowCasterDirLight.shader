
Shader "Custom/ShadowCasterDirLight"
{
    SubShader
    {
        Tags { "RenderType" = "Opaque" }
        Pass
        {
            Cull Front // helps to reduce shadow acne
            Offset 2, 2 // bias = m.tan(θ) + r
            ZWrite On

            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "UnityCG.cginc"

            struct appdata {
                float4 vertex : POSITION;
            };

            struct v2f {
                float4 pos : SV_POSITION;
            };

            v2f vert(appdata v)
            {
                v2f o;
                o.pos = UnityObjectToClipPos(v.vertex); // transform into shadow camera clip space
                return o;
            }

            float frag(v2f i) : SV_Target
            {
                return i.pos.z / i.pos.w; // encode depth automatically from SV_POSITION.z / SV_POSITION

            }

            ENDCG
        }
    }
}
