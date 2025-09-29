Shader "Custom/ShowPointLightShadowMap"
{
    Properties
    {
        _ShadowMap("Shadow Cubemap", Cube) = "" {}
        _FaceIndex("Face Index (0-5)", Range(0, 5)) = 0
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

            samplerCUBE _ShadowMap;
            uniform int _FaceIndex;

            struct appdata
            {
                float4 vertex : POSITION;
                float2 uv : TEXCOORD0;
            };

            struct v2f
            {
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
            
            float3 FaceDir(int face, float2 uv)
            {
                uv = uv * 2 - 1; // [0..1] → [ - 1..1]
                switch(face)
                {
                    case 0 : return normalize(float3(1, uv.y, - uv.x)); // + X
                    case 1 : return normalize(float3(- 1, uv.y, uv.x)); // - X
                    case 2 : return normalize(float3(uv.x, 1, - uv.y)); // + Y
                    case 3 : return normalize(float3(uv.x, - 1, uv.y)); // - Y
                    case 4 : return normalize(float3(uv.x, uv.y, 1)); // + Z
                    case 5 : return normalize(float3(- uv.x, uv.y, - 1)); // - Z
                }
                return float3(0, 0, 1);
            }

            fixed4 frag(v2f i) : SV_Target
            {
                float3 dir = FaceDir(_FaceIndex, i.uv); // map quad UV → cubemap face direction
                float dist = texCUBE(_ShadowMap, dir).r; // sample stored distance (world units)
                return fixed4(dist, dist, dist, 1);
            }
            ENDCG
        }
    }
}
