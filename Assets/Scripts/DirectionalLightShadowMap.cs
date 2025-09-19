using UnityEngine;

[RequireComponent(typeof(Light))]
public class DirectionalLightShadowMap : MonoBehaviour
{
    public int shadowResolution = 1024;

    private Camera shadowCam;
    public RenderTexture shadowMap;
    private Light dirLight;
    private Camera mainCam;

    void Start()
    {
        dirLight = GetComponent<Light>();
        mainCam = Camera.main;

        // Use ARGB32 for debugging (switch to RFloat later)
        shadowMap = new RenderTexture(shadowResolution, shadowResolution, 16, RenderTextureFormat.RFloat);
        shadowMap.wrapMode = TextureWrapMode.Clamp;
        shadowMap.filterMode = FilterMode.Bilinear;

        // Create shadow camera attached to directional light
        GameObject camObj = new GameObject("DirShadowCam");
        camObj.transform.SetParent(transform, false);

        shadowCam = camObj.AddComponent<Camera>();
        shadowCam.enabled = false;
        shadowCam.orthographic = true;
        shadowCam.clearFlags = CameraClearFlags.SolidColor;
        shadowCam.backgroundColor = Color.white;
        shadowCam.targetTexture = shadowMap;

        // Push to global
        Shader.SetGlobalTexture("_DirectionalShadowMap", shadowMap);

        // Assign to quad if found
        GameObject quad = GameObject.Find("Quad");
        if (quad != null)
        {
            var mat = quad.GetComponent<Renderer>().material;
            if (mat != null && mat.HasProperty("_ShadowMap"))
            {
                mat.SetTexture("_ShadowMap", shadowMap);
            }
        }
    }

    void LateUpdate()
    {
        if (mainCam == null || shadowCam == null) return;

        // 1. Get frustum corners from main camera
        Vector3[] frustumCorners = new Vector3[8];
        GetFrustumCornersWorld(mainCam, frustumCorners);

        // 2. Transform corners into light space
        Matrix4x4 lightView = Matrix4x4.TRS(Vector3.zero, transform.rotation, Vector3.one).inverse;
        Vector3 min = Vector3.one * float.MaxValue;
        Vector3 max = Vector3.one * float.MinValue;

        foreach (var c in frustumCorners)
        {
            Vector3 lc = lightView.MultiplyPoint(c);
            min = Vector3.Min(min, lc);
            max = Vector3.Max(max, lc);
        }

        // 3. Fit orthographic camera to cover the frustum
        shadowCam.transform.position = transform.position;  // align with directional light
        shadowCam.transform.rotation = transform.rotation;

        shadowCam.orthographicSize = (max.y - min.y) * 0.5f;
        shadowCam.aspect = (max.x - min.x) / (max.y - min.y);
        shadowCam.nearClipPlane = -max.z;
        shadowCam.farClipPlane = -min.z;

        // 4. Render shadow map
        Shader scShader = Shader.Find("Custom/ShadowCaster");
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
