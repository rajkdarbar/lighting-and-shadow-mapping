Shader "Custom/ShowDirLightShadowMap"
{
    Properties
    {
        _ShadowMap ("Shadow Map", 2D) = "white" {}
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

            struct appdata { float4 vertex : POSITION; float2 uv : TEXCOORD0; };
            struct v2f { float4 pos : SV_POSITION; float2 uv : TEXCOORD0; };

            v2f vert(appdata v)
            {
                v2f o;
                o.pos = UnityObjectToClipPos(v.vertex);
                o.uv = v.uv;
                return o;
            }

            sampler2D _ShadowMap;

            fixed4 frag(v2f i) : SV_Target
            {
                float d = tex2D(_ShadowMap, i.uv).r;
                d = pow(d, 0.2f);
                return float4(d, d, d, 1);
            }
            ENDCG
        }
    }
}
