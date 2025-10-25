using UnityEngine;

[RequireComponent(typeof(Light))]
public class SingleSpotLightShadowMap : MonoBehaviour
{
    public int shadowResolution = 4096;
    [Range(0.0001f, 0.02f)] public float depthBias = 0.001f;

    private Camera shadowCam;
    private Light spot;
    private RenderTexture shadowMap;

    void Start()
    {
        spot = GetComponent<Light>();

        if (spot.type != LightType.Spot)
        {
            Debug.LogError("This script only works with a Spot Light!");
            enabled = false;
            return;
        }

        // Create view-space shadow map (linear distance)
        shadowMap = new RenderTexture(shadowResolution, shadowResolution, 0, RenderTextureFormat.RFloat);
        shadowMap.filterMode = FilterMode.Bilinear;
        shadowMap.wrapMode = TextureWrapMode.Clamp;
        shadowMap.useMipMap = false;
        shadowMap.Create();


        // Setup camera for shadow rendering
        GameObject camObj = new GameObject("SpotShadowCam");
        camObj.transform.SetParent(transform, false);
        shadowCam = camObj.AddComponent<Camera>();
        shadowCam.enabled = false;
        shadowCam.orthographic = false;

        shadowCam.clearFlags = CameraClearFlags.SolidColor;
        shadowCam.backgroundColor = Color.white;
        shadowCam.targetTexture = shadowMap;
        shadowCam.cullingMask = spot.cullingMask;

        // Debug quad for shadow map visualization
        var quad = GameObject.Find("QuadSpotLight");
        if (quad != null)
        {
            var r = quad.GetComponent<Renderer>();
            var m = r.material;
            m.SetTexture("_ShadowMap", shadowMap);
            r.material = m;
        }
    }

    void LateUpdate()
    {
        if (!shadowCam || !spot) return;

        // Sync camera with spotlight
        shadowCam.transform.position = transform.position;
        shadowCam.transform.rotation = transform.rotation;
        shadowCam.transform.forward = transform.forward;

        shadowCam.fieldOfView = spot.spotAngle * 1.05f;
        shadowCam.aspect = 1.0f;
        shadowCam.nearClipPlane = 0.3f;
        shadowCam.farClipPlane = spot.range;

        shadowCam.clearFlags = CameraClearFlags.SolidColor;
        shadowCam.backgroundColor = Color.white;

        // Render with custom shadow caster shader
        var scShader = Shader.Find("Custom/ShadowCasterSpotLight");
        if (scShader != null)
            shadowCam.RenderWithShader(scShader, "RenderType");

        // Compute view and projection matrix and send to shader
        Matrix4x4 view = shadowCam.worldToCameraMatrix;
        Matrix4x4 proj = GL.GetGPUProjectionMatrix(shadowCam.projectionMatrix, true);
        Matrix4x4 vp = proj * view;

        Shader.SetGlobalMatrix("_SpotLight_VP_ShadowCaster", vp);
        Shader.SetGlobalMatrix("_SpotLight_View_ShadowCaster", view);
        Shader.SetGlobalFloat("_SpotLightFar_ShadowCaster", shadowCam.farClipPlane);


        // Find all renderers using Blinn–Phong shader
        var renderers = FindObjectsOfType<Renderer>();
        foreach (var r in renderers)
        {
            var m = r.sharedMaterial;
            if (m && m.shader.name == "Custom/BlinnPhong")
            {
                m.SetTexture("_SpotLightShadowMap", shadowMap);
                m.SetMatrix("_SpotLightVP", vp);
                m.SetMatrix("_SpotLightV", view);
                m.SetFloat("_SpotLightShadowMapSize", shadowResolution);
                m.SetFloat("_SpotLightNear", shadowCam.nearClipPlane);
                m.SetFloat("_SpotLightFar", shadowCam.farClipPlane);
                m.SetFloat("_DepthBiasSpotlight", depthBias);
            }
        }
    }

    void OnEnable() => Shader.SetGlobalFloat("_UseSpotShadow", 1);
    void OnDisable() => Shader.SetGlobalFloat("_UseSpotShadow", 0);

}
