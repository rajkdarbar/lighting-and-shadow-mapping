
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

            // Directional Light
            uniform float3 _DirectionalLightColor;
            uniform float3 _DirectionalLightDir;
            uniform float _DirectionalLightIntensity;

            sampler2D _DirLightShadowMap;
            float4x4 _DirLightViewProjectionMatrix;
            float _DepthBias, _NormalBias, _DirLightShadowMapSize;

            // Point lights
            uniform int _NumPointLights;
            uniform float3 _PointLightPos[4];
            uniform float3 _PointLightColor[4];
            uniform float _PointLightIntensity[4];
            uniform float _PointLightRange[4];

            // Spot lights
            uniform int _NumSpotLights;
            uniform float3 _SpotLightPos[4];
            uniform float3 _SpotLightDir[4];
            uniform float3 _SpotLightColor[4];
            uniform float _SpotLightIntensity[4];
            uniform float _SpotLightRange[4];
            uniform float _SpotLightAngle[4];
            uniform float _SpotLightShadowMapSize;

            float4x4 _SpotLightViewProjectionMatrix[4];
            UNITY_DECLARE_TEX2DARRAY(_SpotLightShadowMaps);



            v2f vert (appdata v)
            {
                v2f o;
                o.pos = UnityObjectToClipPos(v.vertex);
                o.normal = UnityObjectToWorldNormal(v.normal);
                o.worldPos = mul(unity_ObjectToWorld, v.vertex).xyz;

                return o;
            }

            // -- -- Directional light shadow test -- --
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
                float texel = 1.0 / _DirLightShadowMapSize;
                float bias = (_DepthBias + _NormalBias * (1.0 - ndl)) * texel;

                /*

                // This part is for hard shadow edges

                float currDepth = lp.z;
                float shadowMapDepth = tex2D(_DirLightShadowMap, uv).r;

                // 0 = shadow, 1 = lit
                #if defined(UNITY_REVERSED_Z)
                return (currDepth <= shadowMapDepth + bias) ? 0.0 : 1.0;
                #else
                return (currDepth >= shadowMapDepth + bias) ? 0.0 : 1.0;
                #endif

                */


                // -- -- -- -- PCF 5×5 -- -- -- --
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


            // -- -- Spotlight shadow test -- --
            float ShadowFactorSpotLight(int index, float3 worldPos, float3 normal)
            {
                // Transform world position into the spotlight’s clip space
                float4 lp = mul(_SpotLightViewProjectionMatrix[index], float4(worldPos, 1));
                lp.xyz /= lp.w; // perspective divide → NDC [ - 1, 1]

                // Convert to UV [0, 1]
                float2 uv = lp.xy * 0.5 + 0.5;

                #if UNITY_UV_STARTS_AT_TOP
                uv.y = 1.0 - uv.y;
                #endif

                // Outside shadow map → fully lit
                if (uv.x < 0 || uv.x > 1 || uv.y < 0 || uv.y > 1)
                return 1.0;

                // Bias (similar to directional)
                float3 L = normalize(_SpotLightPos[index] - worldPos);
                float ndl = saturate(dot(normalize(normal), L));
                float texel = 1.0 / _SpotLightShadowMapSize;
                float bias = (_DepthBias + _NormalBias * (1.0 - ndl)) * texel;

                // Current fragment depth in light space
                float currDepth = lp.z;

                // PCF 5×5
                float shadow = 0.0;
                int samples = 0;

                for (int x = - 2; x <= 2; x ++)
                {
                    for (int y = - 2; y <= 2; y ++)
                    {
                        float2 offset = float2(x, y) * texel;
                        float2 sampleUV = uv + offset;
                                                
                        float shadowMapDepth = UNITY_SAMPLE_TEX2DARRAY(_SpotLightShadowMaps, float3(sampleUV, index)).r;

                        #if defined(UNITY_REVERSED_Z)
                        shadow += (currDepth <= shadowMapDepth + bias) ? 0.0 : 1.0;
                        #else
                        shadow += (currDepth >= shadowMapDepth + bias) ? 0.0 : 1.0;
                        #endif

                        samples ++;
                    }
                }

                return shadow / samples; // 0 = shadowed, 1 = lit
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
                if (_NumPointLights > 0)
                {
                    for (int p = 0; p < _NumPointLights; p ++)
                    {
                        float3 L = _PointLightPos[p] - i.worldPos;
                        float dist = length(L);
                        L = normalize(L);

                        float attenuation = saturate(1.0 - dist / _PointLightRange[p]);

                        float diff = max(0, dot(N, L));
                        float3 H = normalize(L + V);
                        float spec = pow(max(0, dot(N, H)), _Shininess);

                        totalLight += attenuation * (
                        _Kd.rgb * _PointLightColor[p] * _PointLightIntensity[p] * diff +
                        _Ks.rgb * _PointLightColor[p] * _PointLightIntensity[p] * spec
                        );
                    }
                }


                // Spot lights contribution
                if (_NumSpotLights > 0)
                {
                    for (int s = 0; s < _NumSpotLights; s ++)
                    {
                        float3 L = _SpotLightPos[s] - i.worldPos;
                        float dist = length(L);
                        L = normalize(L);

                        float spotFactor = dot(normalize(- _SpotLightDir[s]), L); // Unity's forward is opposite
                        float cutoff = _SpotLightAngle[s];
                        float spotAtten = (spotFactor > cutoff) ? spotFactor : 0;

                        float attenuation = saturate(1.0 - dist / _SpotLightRange[s]);

                        float diff = max(0, dot(N, L));
                        float3 H = normalize(L + V);
                        float spec = pow(max(0, dot(N, H)), _Shininess);

                        // -- -- Shadow test for this spotlight -- --
                        float shadow = ShadowFactorSpotLight(s, i.worldPos, i.normal);

                        totalLight += shadow * spotAtten * attenuation * (
                        _Kd.rgb * _SpotLightColor[s] * _SpotLightIntensity[s] * diff +
                        _Ks.rgb * _SpotLightColor[s] * _SpotLightIntensity[s] * spec);
                    }
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
