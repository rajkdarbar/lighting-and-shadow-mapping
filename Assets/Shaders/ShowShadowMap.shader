Shader "Custom/ShowShadowMap"
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
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "UnityCG.cginc"

            struct appdata {
                float4 vertex : POSITION;
                float2 uv : TEXCOORD0;
            };

            struct v2f {
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

            sampler2D _ShadowMap;

            fixed4 frag(v2f i) : SV_Target
            {
                // sample red channel
                float depth = tex2D(_ShadowMap, i.uv).r;

                // optional contrast boost
                depth = pow(depth, 10);

                return fixed4(depth, depth, depth, 1);
            }
            ENDCG
        }
    }
}
