
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

            float _UseDirShadow;
            float _UseSpotShadow;
            float _UsePointShadow;

            // Material properties
            fixed4 _Ka;
            fixed4 _Kd;
            fixed4 _Ks;
            float _Shininess;

            // Ambient light
            fixed4 _AmbientColor;
            float _AmbientIntensity;

            // Directional light
            float3 _DirectionalLightColor;
            float3 _DirectionalLightDir;
            float _DirectionalLightIntensity;

            sampler2D _DirLightShadowMap;
            float _DirLightShadowMapSize;
            float4x4 _DirLightViewProjectionMatrix;
            float _DepthBiasDirLight;

            // Spot light
            float3 _SpotLightPos;
            float3 _SpotLightDir;
            float3 _SpotLightColor;
            float _SpotLightIntensity;
            float _SpotLightRange;
            float _SpotLightAngle;
            float _SpotLightShadowMapSize;
            float _SpotLightNear;
            float _SpotLightFar;
            float _DepthBiasSpotlight, _NormalBiasSpotlight;

            sampler2D _SpotLightShadowMap;
            float4x4 _SpotLightVP;
            float4x4 _SpotLightV;

            // Point light
            float3 _PointLightPos;
            float3 _PointLightColor;
            float _PointLightIntensity;
            float _PointLightRange;
            float _DepthBiasPointLight;

            UNITY_DECLARE_TEXCUBE(_PointLightShadowMap);
            float _PointLightShadowMapSize;



            struct appdata
            {
                float4 vertex : POSITION;
                float3 normal : NORMAL;
            };

            struct v2f
            {
                float4 pos : SV_POSITION;
                float3 normal : TEXCOORD0;
                float3 worldPos : TEXCOORD1;
            };


            v2f vert (appdata v)
            {
                v2f o;

                o.pos = UnityObjectToClipPos(v.vertex);
                o.normal = UnityObjectToWorldNormal(v.normal);
                o.worldPos = mul(unity_ObjectToWorld, v.vertex).xyz;

                return o;
            }

            // Directional light shadow test
            float ShadowFactorDirectionalLight(float3 worldPos, float3 normal)
            {
                float4 clipPos = mul(_DirLightViewProjectionMatrix, float4(worldPos, 1));
                float3 ndc = clipPos.xyz / clipPos.w; // x, y ∈ [ - 1, 1], but z ∈ [0, 1]

                float2 uv = ndc.xy * 0.5 + 0.5; // maps from [ - 1, 1] → [0, 1]

                #if UNITY_UV_STARTS_AT_TOP
                uv.y = 1.0 - uv.y; // when sampling the shadow map, we always assume (0, 0) = bottom - left
                #endif

                // Outside shadow map
                if (uv.x<0||uv.x>1||uv.y<0||uv.y>1)
                return 0.0;

                // Slope - scaled depth bias
                float texel = 1.0 / _DirLightShadowMapSize;
                float3 L = normalize(- _DirectionalLightDir);
                float3 N = normalize(normal);
                float NdotL = saturate(dot(N, L));
                float bias = (_DepthBiasDirLight * (1.0 - NdotL)) * texel;
                bias = max(bias, 0.0001);

                // PCF 5×5
                float currDepth = ndc.z; // already in [0, 1]

                float shadow = 0.0;
                int samples = 0;

                // sample in a 5x5 grid around uv
                for (int x = - 2; x <= 2; x ++)
                {
                    for (int y = - 2; y <= 2; y ++)
                    {
                        float2 offset = float2(x, y) * texel;
                        float2 sampleUV = uv + offset;
                        float shadowMapDepth = tex2D(_DirLightShadowMap, sampleUV).r; // [0, 1] range

                        // 0 = shadow, 1 = lit
                        #if defined(UNITY_REVERSED_Z)
                        shadow += (currDepth <= shadowMapDepth + bias) ? 0.0 : 1.0;
                        #else
                        shadow += (currDepth >= shadowMapDepth + bias) ? 0.0 : 1.0;
                        #endif

                        samples ++;
                    }
                }

                return shadow / samples; // 0 = fully shadowed, 1 = fully lit, values in between = soft edge
            }



            // Spotlight shadow test using view - space depth
            float ShadowFactorSpotLight(float3 worldPos, float3 normal)
            {
                // Transform world position into light view - space
                float3 lightViewPos = mul(_SpotLightV, float4(worldPos, 1.0)).xyz;

                // Project to clip space for UV
                float4 clipPos = mul(_SpotLightVP, float4(worldPos, 1.0));
                float3 ndc = clipPos.xyz / clipPos.w;

                float2 uv = ndc.xy * 0.5 + 0.5;

                #if UNITY_UV_STARTS_AT_TOP
                uv.y = 1.0 - uv.y;
                #endif

                // Outside shadow map
                if (uv.x < 0 || uv.x > 1 || uv.y < 0 || uv.y > 1)
                return 0.0;

                // Current fragment’s depth in light’s view - space
                float currDepth = length(lightViewPos); // distance from spotlight
                currDepth /= _SpotLightFar; // normalize to [0, 1]

                // Slope - scaled depth bias
                float texel = 1.0 / _SpotLightShadowMapSize;
                float3 L = normalize(_SpotLightDir);
                float3 N = normalize(normal);
                float NdotL = saturate(dot(N, L));
                float bias = _DepthBiasSpotlight * (1.0 - NdotL) * texel;
                bias = max(bias, 0.0001);

                // PCF 5×5
                float shadow = 0.0;
                int samples = 0;

                for (int x = - 2; x <= 2; x ++)
                {
                    for (int y = - 2; y <= 2; y ++)
                    {
                        float2 offset = float2(x, y) * texel;
                        float2 sampleUV = uv + offset;

                        float shadowMapDepth = tex2D(_SpotLightShadowMap, sampleUV).r;
                        shadow += (currDepth > shadowMapDepth + bias) ? 0.0 : 1.0;

                        samples ++;
                    }
                }

                return shadow / samples; // 1 = lit, 0 = shadowed
            }


            // Point light shadow test
            float ShadowFactorPointLight(float3 worldPos, float3 normal)
            {
                float3 L = worldPos - _PointLightPos; // vector from light to fragment
                float3 dir = normalize(L);

                float distToLight = length(L);
                float distToLightNorm = saturate(distToLight / _PointLightRange); // normalize distance [0, 1]

                // Slope - scaled depth bias
                float texelSize = 1.0 / _PointLightShadowMapSize;
                float ndl = saturate(dot(normal, - dir));
                float bias = _DepthBiasPointLight * (1.0 - ndl) + texelSize; // adding texelSize instead of multiplying to avoid arc / ring patterns on the floor
                bias = max(bias, 0.0001);

                // Poisson jitter offsets (16 samples)
                float3 poisson[16] =
                {
                    float3(0.355, 0.355, 0),
                    float3(- 0.355, 0.355, 0),
                    float3(0.355, - 0.355, 0),
                    float3(- 0.355, - 0.355, 0),
                    float3(0.707, 0, 0.707),
                    float3(- 0.707, 0, 0.707),
                    float3(0.707, 0, - 0.707),
                    float3(- 0.707, 0, - 0.707),
                    float3(0, 0.707, 0.707),
                    float3(0, - 0.707, 0.707),
                    float3(0, 0.707, - 0.707),
                    float3(0, - 0.707, - 0.707),
                    float3(0.577, 0.577, 0.577),
                    float3(- 0.577, 0.577, 0.577),
                    float3(0.577, - 0.577, 0.577),
                    float3(0.577, 0.577, - 0.577)
                };

                float shadow = 0.0;

                for (int i = 0; i < 16; i ++)
                {
                    float3 sampleDir = normalize(dir + poisson[i] * texelSize);
                    float shadowDistNorm = UNITY_SAMPLE_TEXCUBE(_PointLightShadowMap, sampleDir).r;

                    shadow += (distToLightNorm >= shadowDistNorm + bias) ? 0.0 : 1.0;
                }

                return shadow / 16.0;
            }



            // Directional light contributions
            float3 ComputeDirectionalLight(float3 N, float3 V, float3 worldPos)
            {
                float3 total = 0;

                float3 L = normalize(- _DirectionalLightDir); // Unity’s light forward points opposite to light direction

                float diff = max(0, dot(N, L));
                float3 H = normalize(L + V);
                float NdotH = max(0, dot(N, H));
                float spec = pow(NdotH, _Shininess);  

                float shadow = ShadowFactorDirectionalLight(worldPos, N);

                total += shadow * (_Kd.rgb * _DirectionalLightColor * _DirectionalLightIntensity * diff +
                _Ks.rgb * _DirectionalLightColor * _DirectionalLightIntensity * spec);

                return total;
            }


            // Spotlight contribution
            float3 ComputeSpotLight(float3 N, float3 V, float3 worldPos)
            {
                float3 total = 0;

                float3 L = _SpotLightPos - worldPos;
                float dist = length(L);
                L = normalize(L);

                // Cone (angular) attenuation
                float spotFactor = dot(normalize(_SpotLightDir), - L);
                float cutoff = _SpotLightAngle;
                float smoothEdge = lerp(cutoff, 1.0, 0.15);
                float coneAtten = saturate((spotFactor - cutoff) / (smoothEdge - cutoff));
                coneAtten = pow(coneAtten, 3.0);

                // Distance attenuation
                float rangeAtten = saturate(1.0 - dist / _SpotLightRange);

                // Lighting
                float diff = max(0, dot(N, L));
                float3 H = normalize(L + V);
                float NdotH = max(0, dot(N, H));
                float spec = pow(NdotH, _Shininess);  
                
                // Shadow calculation
                float shadow = ShadowFactorSpotLight(worldPos, N);

                total += shadow * coneAtten * rangeAtten * (
                _Kd.rgb * _SpotLightColor * _SpotLightIntensity * diff +
                _Ks.rgb * _SpotLightColor * _SpotLightIntensity * spec);

                return total;
            }


            // Point light contribution
            float3 ComputePointLight(float3 N, float3 V, float3 worldPos)
            {
                float3 total = 0;

                float3 L = _PointLightPos - worldPos;
                float dist = length(L);
                L = normalize(L);

                float attenuation = saturate(1.0 - dist / _PointLightRange);

                float diff = max(0, dot(N, L));
                float3 H = normalize(L + V);
                float NdotH = max(0, dot(N, H));
                float spec = pow(NdotH, _Shininess);                

                float shadow = ShadowFactorPointLight(worldPos, N);
                // float shadow = 1;

                total += shadow * attenuation * (
                _Kd.rgb * _PointLightColor * _PointLightIntensity * diff +
                _Ks.rgb * _PointLightColor * _PointLightIntensity * spec);

                return total;
            }


            float4 frag(v2f i) : SV_Target
            {
                float3 totalLight = 0;

                float3 N = normalize(i.normal);
                float3 V = normalize(_WorldSpaceCameraPos - i.worldPos);

                if (_UseDirShadow > 0.5)
                totalLight += ComputeDirectionalLight(N, V, i.worldPos);

                if (_UseSpotShadow > 0.5)
                totalLight += ComputeSpotLight(N, V, i.worldPos);

                if (_UsePointShadow > 0.5)
                totalLight += ComputePointLight(N, V, i.worldPos);

                float3 ambient = _Ka.rgb * _AmbientColor.rgb * _AmbientIntensity; // ambient light contribution

                return float4(ambient + totalLight, 1.0);
            }

            ENDCG
        }
    }
}
