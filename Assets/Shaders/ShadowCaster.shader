
Shader "Custom/ShadowCaster"
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
                float depth : TEXCOORD0;
            };

            float4x4 _DirLightViewProjectionMatrix; // coming from DirectionalLightShadowMap.cs

            v2f vert(appdata v)
            {
                v2f o;
                o.pos = UnityObjectToClipPos(v.vertex); // render from shadowCam

                float4 worldPos = mul(unity_ObjectToWorld, v.vertex);
                float4 lpos = mul(_DirLightViewProjectionMatrix, worldPos);
                o.depth = lpos.z / lpos.w; // NDC z mapped to [0..1] because we used 'true' in GetGPUProjectionMatrix
                return o;
            }

            fixed4 frag(v2f i) : SV_Target
            {
                float d = saturate(i.depth); // write depth to R
                return float4(d, d, d, 1);
            }
            ENDCG
        }
    }
}
