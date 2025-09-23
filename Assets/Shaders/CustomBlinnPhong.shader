
Shader "Custom/BlinnPhong"
{
    Properties
    {
        _Ka("Ambient Reflectance (Ka)", Color) = (1, 1, 1, 1)
        _Kd("Diffuse Reflectance (Kd)", Color) = (1, 1, 1, 1)
        _Ks("Specular Reflectance (Ks)", Color) = (1, 1, 1, 1)
        _Shininess("Shininess", Range(1, 128)) = 32

        [Space(15)]

        _AmbientColor("Ambient Light Color", Color) = (1, 1, 1, 1)
        _AmbientIntensity("Ambient Light Intensity", Range(0, 2)) = 1.0

        _DepthBias("Depth Bias", Range(0, 3)) = 1
        _NormalBias("Normal Bias", Range(0, 3)) = 1
    }

    SubShader
    {
        Tags { "RenderType" = "Opaque" }
        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "UnityCG.cginc"

            struct appdata {
                float4 vertex : POSITION;
                float3 normal : NORMAL;
            };

            struct v2f {
                float4 pos : SV_POSITION;
                float3 normal : TEXCOORD0;
                float3 worldPos : TEXCOORD1;
            };

            // Material properties
            fixed4 _Ka;
            fixed4 _Kd;
            fixed4 _Ks;
            float _Shininess;

            // Ambient light
            fixed4 _AmbientColor;
            float _AmbientIntensity;

            // Light counts
            #define MAX_POINT_LIGHTS 3
            #define MAX_SPOT_LIGHTS 2

            // Directional Light
            uniform float3 _DirectionalLightColor;
            uniform float3 _DirectionalLightDir;
            uniform float _DirectionalLightIntensity;

            // Shadow map (from DirectionalLightShadowMap.cs)
            sampler2D _DirLightShadowMap;
            float4x4 _DirLightViewProjectionMatrix;
            float _DepthBias, _NormalBias, _ShadowMapSize;

            // Point lights
            uniform int _NumPointLights;
            uniform float3 _PointLightPos[MAX_POINT_LIGHTS];
            uniform float3 _PointLightColor[MAX_POINT_LIGHTS];
            uniform float _PointLightIntensity[MAX_POINT_LIGHTS];
            uniform float _PointLightRange[MAX_POINT_LIGHTS]; // for attenuation

            // Spot lights
            uniform int _NumSpotLights;
            uniform float3 _SpotLightPos[MAX_SPOT_LIGHTS];
            uniform float3 _SpotLightDir[MAX_SPOT_LIGHTS];
            uniform float3 _SpotLightColor[MAX_SPOT_LIGHTS];
            uniform float _SpotLightIntensity[MAX_SPOT_LIGHTS];
            uniform float _SpotLightRange[MAX_SPOT_LIGHTS];
            uniform float _SpotLightAngle[MAX_SPOT_LIGHTS]; // cutoff cone


            v2f vert (appdata v)
            {
                v2f o;
                o.pos = UnityObjectToClipPos(v.vertex);
                o.normal = UnityObjectToWorldNormal(v.normal);
                o.worldPos = mul(unity_ObjectToWorld, v.vertex).xyz;

                return o;
            }

            float ShadowFactorDirectionalLight(float3 worldPos, float3 normal)
            {
                float4 lp = mul(_DirLightViewProjectionMatrix, float4(worldPos, 1));
                lp.xyz /= lp.w; // x, y, z ∈ [ - 1, 1]

                float2 uv = lp.xy * 0.5 + 0.5; // maps from [ - 1, 1] → [0, 1]

                #if UNITY_UV_STARTS_AT_TOP
                uv.y = 1.0 - uv.y; // when sampling the shadow map, we always assume (0, 0) = bottom - left
                #endif

                // Outside shadow map
                if (uv.x<0||uv.x>1||uv.y<0||uv.y>1) return 1.0;

                // slope - scaled depth bias
                float3 L = normalize(- _DirectionalLightDir);
                float ndl = saturate(dot(normalize(normal), L));
                float texel = 1.0 / _ShadowMapSize;
                float bias = (_DepthBias + _NormalBias * (1.0 - ndl)) * texel;

                /*

                float currDepth = lp.z;
                float shadowMapDepth = tex2D(_DirLightShadowMap, uv).r;

                // 0 = shadow, 1 = lit
                #if defined(UNITY_REVERSED_Z)
                return (currDepth <= shadowMapDepth + bias) ? 0.0 : 1.0;
                #else
                return (currDepth >= shadowMapDepth + bias) ? 0.0 : 1.0;
                #endif

                */


                // -- -- -- -- PCF 3×3 -- -- -- --
                float currDepth = lp.z;

                float shadow = 0.0;
                int samples = 0;

                // sample in a 5x5 grid around uv
                for (int x = - 2; x <= 2; x ++)
                {
                    for (int y = - 2; y <= 2; y ++)
                    {
                        float2 offset = float2(x, y) * texel;
                        float2 sampleUV = uv + offset;
                        float shadowMapDepth = tex2D(_DirLightShadowMap, sampleUV).r;

                        // 0 = shadow, 1 = lit
                        #if defined(UNITY_REVERSED_Z)
                        shadow += (currDepth <= shadowMapDepth + bias) ? 0.0 : 1.0;
                        #else
                        shadow += (currDepth >= shadowMapDepth + bias) ? 0.0 : 1.0;
                        #endif

                        samples ++;
                    }
                }

                // average all samples
                return shadow / samples; // 0 = fully shadowed, 1 = fully lit, values in between = soft edge
            }


            float4 frag (v2f i) : SV_Target
            {
                float3 totalLight = 0;

                // Directional light contributions
                float3 L = normalize(- _DirectionalLightDir); // Unity’s light “forward” points opposite to light direction
                float3 N = normalize(i.normal);
                float3 V = normalize(_WorldSpaceCameraPos - i.worldPos);
                float diff = max(0, dot(N, L));
                float3 H = normalize(L + V);
                float spec = pow(max(0, dot(N, H)), _Shininess);

                float shadow = ShadowFactorDirectionalLight(i.worldPos, i.normal);

                totalLight += shadow * (_Kd.rgb * _DirectionalLightColor * _DirectionalLightIntensity * diff +
                _Ks.rgb * _DirectionalLightColor * _DirectionalLightIntensity * spec);


                // Point lights contribution
                for (int p = 0; p < _NumPointLights; p ++)
                {
                    float3 L = _PointLightPos[p] - i.worldPos;
                    float dist = length(L);
                    L = normalize(L);

                    float attenuation = saturate(1.0 - dist / _PointLightRange[p]);

                    float diff = max(0, dot(N, L));
                    float3 H = normalize(L + V);
                    float spec = pow(max(0, dot(N, H)), _Shininess);

                    totalLight += attenuation * (_Kd.rgb * _PointLightColor[p] * _PointLightIntensity[p] * diff +
                    _Ks.rgb * _PointLightColor[p] * _PointLightIntensity[p] * spec);
                }

                // Spot lights contribution
                for (int s = 0; s < _NumSpotLights; s ++)
                {
                    float3 L = _SpotLightPos[s] - i.worldPos;
                    float dist = length(L);
                    L = normalize(L);

                    float spotFactor = dot(normalize(- _SpotLightDir[s]), L); // Unity’s light “forward” points opposite to light direction
                    float cutoff = _SpotLightAngle[s];
                    float spotAtten = (spotFactor > cutoff) ? spotFactor : 0;

                    float attenuation = saturate(1.0 - dist / _SpotLightRange[s]);

                    float diff = max(0, dot(N, L));
                    float3 H = normalize(L + V);
                    float spec = pow(max(0, dot(N, H)), _Shininess);

                    totalLight += spotAtten * attenuation * (
                    _Kd.rgb * _SpotLightColor[s] * _SpotLightIntensity[s] * diff +
                    _Ks.rgb * _SpotLightColor[s] * _SpotLightIntensity[s] * spec);
                }

                // Ambient light contributions
                float3 ambient = _Ka.rgb * _AmbientColor.rgb * _AmbientIntensity;
                float3 result = ambient + totalLight;
                return float4(result, 1.0);
            }
            ENDCG
        }
    }
}
