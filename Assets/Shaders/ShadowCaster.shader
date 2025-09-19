Shader "Custom/ShadowCaster"
{
    SubShader
    {
        Tags { "RenderType" = "Opaque" }
        Pass
        {
            Cull Off
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
                float depth : TEXCOORD0;
            };

            v2f vert(appdata v)
            {
                v2f o;
                o.pos = UnityObjectToClipPos(v.vertex);

                // NDC depth [ - 1, 1] → [0, 1]
                float ndcDepth = o.pos.z / o.pos.w;
                o.depth = ndcDepth * 0.5 + 0.5;

                return o;
            }

            float4 frag(v2f i) : SV_Target
            {
                // write depth into RED channel
                return float4(i.depth, i.depth, i.depth, 1); // grayscale
            }
            ENDCG
        }
    }
}
