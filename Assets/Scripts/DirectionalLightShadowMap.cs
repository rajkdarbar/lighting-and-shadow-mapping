
using UnityEngine;

[RequireComponent(typeof(Light))]
public class DirectionalLightShadowMap : MonoBehaviour
{
    public int shadowResolution = 4096;

    private Camera shadowCam;
    public RenderTexture shadowMap;
    private Light dirLight;
    private Camera mainCam;

    void Start()
    {
        dirLight = GetComponent<Light>();
        mainCam = Camera.main;

        // Initialize the RenderTexture that will store the shadow map
        shadowMap = new RenderTexture(shadowResolution, shadowResolution, 16, RenderTextureFormat.RFloat);
        shadowMap.useMipMap = false;
        shadowMap.filterMode = FilterMode.Bilinear;
        shadowMap.wrapMode = TextureWrapMode.Clamp; // control how textures behave when UV coordinates go outside the 0–1 range
        shadowMap.Create();

        // Create shadow camera attached to directional light
        GameObject camObj = new GameObject("DirShadowCam");
        camObj.transform.SetParent(transform, false);
        shadowCam = camObj.AddComponent<Camera>();
        shadowCam.enabled = false;
        shadowCam.orthographic = true;
        shadowCam.clearFlags = CameraClearFlags.SolidColor;
        shadowCam.backgroundColor = SystemInfo.usesReversedZBuffer ? Color.black : Color.white; // ensures empty pixels = “far away”
        shadowCam.targetTexture = shadowMap;

        // Push to global
        Shader.SetGlobalFloat("_DirLightShadowMapSize", shadowResolution);
        Shader.SetGlobalTexture("_DirLightShadowMap", shadowMap);

        var quad = GameObject.Find("QuadDirLight");
        if (quad != null)
        {
            var m = new Material(Shader.Find("Custom/ShowDirLightShadowMap"));
            m.SetTexture("_ShadowMap", shadowMap);
            quad.GetComponent<Renderer>().material = m;
        }
    }

    void LateUpdate()
    {
        if (mainCam == null || shadowCam == null || !dirLight.enabled) return;

        // Frustum corners of main camera in world space
        Vector3[] frustumCorners = new Vector3[8];
        GetFrustumCornersWorld(mainCam, frustumCorners);

        // Compute light-space AABB for those corners (using current rotation)
        Matrix4x4 lightView = shadowCam.worldToCameraMatrix;
        Vector3 min = Vector3.one * float.MaxValue;
        Vector3 max = Vector3.one * float.MinValue;
        for (int i = 0; i < 8; i++)
        {
            Vector3 p = lightView.MultiplyPoint(frustumCorners[i]);
            min = Vector3.Min(min, p);
            max = Vector3.Max(max, p);
        }

        // Center the shadow camera on that AABB
        Vector3 centerLS = 0.5f * (min + max);
        Vector3 centerWS = shadowCam.cameraToWorldMatrix.MultiplyPoint(centerLS);
        shadowCam.transform.position = centerWS;

        // Recompute AABB with the camera now centered (so min/max are symmetric-ish)
        lightView = shadowCam.worldToCameraMatrix;
        min = Vector3.one * float.MaxValue;
        max = Vector3.one * float.MinValue;
        for (int i = 0; i < 8; i++)
        {
            Vector3 p = lightView.MultiplyPoint(frustumCorners[i]);
            min = Vector3.Min(min, p);
            max = Vector3.Max(max, p);
        }

        // Configure orthographic projection from this AABB
        float pad = 0.5f; // small padding to avoid clipping/shimmer
        float width = (max.x - min.x) + 2f * pad;
        float height = (max.y - min.y) + 2f * pad;
        float depth = (max.z - min.z) + 2f * pad;

        shadowCam.orthographicSize = height * 0.5f;
        shadowCam.aspect = width / height;

        // In camera space, Unity looks down -Z; near/far are positive distances
        shadowCam.nearClipPlane = -(max.z) - pad; // max.z is the most negative (closest) in front
        shadowCam.farClipPlane = -(min.z) + pad; // min.z is the farthest

        // Upload the exact VP the shadowCam will render with
        Matrix4x4 proj = GL.GetGPUProjectionMatrix(shadowCam.projectionMatrix, true);
        Matrix4x4 lightVP = proj * lightView;

        // Push to global
        Shader.SetGlobalMatrix("_DirLightViewProjectionMatrix", lightVP);

        // Render depth to the shadow map
        Shader scShader = Shader.Find("Custom/ShadowCasterDirLight");
        if (scShader != null)
        {
            shadowCam.RenderWithShader(scShader, "RenderType");
        }
    }

    void GetFrustumCornersWorld(Camera cam, Vector3[] outCorners)
    {
        Matrix4x4 camLocalToWorld = cam.transform.localToWorldMatrix;

        Vector3[] nearCorners = new Vector3[4];
        Vector3[] farCorners = new Vector3[4];

        cam.CalculateFrustumCorners(new Rect(0, 0, 1, 1), cam.nearClipPlane, Camera.MonoOrStereoscopicEye.Mono, nearCorners);
        cam.CalculateFrustumCorners(new Rect(0, 0, 1, 1), cam.farClipPlane, Camera.MonoOrStereoscopicEye.Mono, farCorners);

        for (int i = 0; i < 4; i++)
        {
            outCorners[i] = camLocalToWorld.MultiplyPoint(nearCorners[i]);
            outCorners[i + 4] = camLocalToWorld.MultiplyPoint(farCorners[i]);
        }
    }
}