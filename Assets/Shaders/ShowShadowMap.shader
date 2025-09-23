
Shader "Custom/ShowShadowMap"
{
    Properties
    {
        _ShadowMap("Shadow Map", 2D) = "white" {}
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

            struct appdata{
                float4 vertex : POSITION;
                float2 uv : TEXCOORD0;
            };

            struct v2f{
                float4 pos : SV_POSITION;
                float2 uv : TEXCOORD0;
            };

            sampler2D _ShadowMap;

            v2f vert(appdata v){
                v2f o;
                o.pos = UnityObjectToClipPos(v.vertex);
                o.uv = v.uv; // quad UVs (0..1) → fills entire quad
                return o;
            }

            fixed4 frag(v2f i) : SV_Target
            {
                float d = tex2D(_ShadowMap, i.uv).r; // 0..1 depth
                d = pow(d, 1.2f);
                return fixed4(d, d, d, 1);
            }
            ENDCG
        }
    }
}
