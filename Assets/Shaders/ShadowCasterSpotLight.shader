Shader "Custom/ShadowCasterSpotLight"
{
    SubShader
    {
        Tags { "RenderType" = "Opaque" }
        Pass
        {
            Cull Front
            Offset 2, 2
            ZWrite On
            ColorMask 0 // don’t write colors

            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "UnityCG.cginc"

            struct appdata { float4 vertex : POSITION; };
            struct v2f { float4 pos : SV_POSITION; };

            v2f vert(appdata v)
            {
                v2f o;
                o.pos = UnityObjectToClipPos(v.vertex);
                return o;
            }

            float frag(v2f i) : SV_Target
            {
                return 0; // ignored, only depth buffer matters
            }
            ENDCG
        }
    }
}
