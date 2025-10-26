using UnityEngine;

public class PointLightShadowMap : MonoBehaviour
{
    public RenderTexture shadowCubemap;
    public int shadowResolution = 4096;
    [Range(0.0001f, 0.1f)] public float depthBias = 0.01f;

    private Light pointLight;
    private Camera shadowCam;
    private static Shader shadowCasterShader;

    void Start()
    {
        pointLight = GetComponent<Light>();
        shadowCasterShader = Shader.Find("Custom/ShadowCasterPointLight");

        // Create cubemap render texture
        shadowCubemap = new RenderTexture(shadowResolution, shadowResolution, 0, RenderTextureFormat.RFloat);
        shadowCubemap.dimension = UnityEngine.Rendering.TextureDimension.Cube;
        shadowCubemap.useMipMap = false;
        shadowCubemap.autoGenerateMips = false;
        shadowCubemap.Create();

        // Shadow Camera
        GameObject camObj = new GameObject("ShadowCam_" + gameObject.name);
        camObj.transform.SetParent(transform, false);
        shadowCam = camObj.AddComponent<Camera>();
        shadowCam.enabled = false;
        shadowCam.orthographic = false;

        shadowCam.clearFlags = CameraClearFlags.SolidColor;
        shadowCam.backgroundColor = Color.white; // far = 1.0

        shadowCam.nearClipPlane = 0.1f;
        shadowCam.farClipPlane = pointLight.range;
        shadowCam.fieldOfView = 90f; // 90° per cube face
        shadowCam.aspect = 1f;

        // Debug quad for shadow map visualization
        var quad = GameObject.Find("QuadPointLight");
        if (quad != null)
        {
            var r = quad.GetComponent<Renderer>();
            var m = r.material;
            m.SetTexture("_ShadowMap", shadowCubemap);
            r.material = m;
        }

        // Push to global
        Shader.SetGlobalFloat("_PointLightShadowMapSize", shadowResolution);
    }

    void LateUpdate()
    {
        RenderShadows();
    }

    public void RenderShadows()
    {
        if (!pointLight || !pointLight.enabled) return;

        for (int face = 0; face < 6; face++)
        {
            shadowCam.transform.position = transform.position;
            shadowCam.transform.rotation = GetCubemapFaceRotation(face);

            shadowCam.clearFlags = CameraClearFlags.SolidColor;
            shadowCam.backgroundColor = Color.white;

            // Projection and view
            Matrix4x4 proj = Matrix4x4.Perspective(90f, 1f, shadowCam.nearClipPlane, shadowCam.farClipPlane);
            Matrix4x4 view = shadowCam.worldToCameraMatrix;
            Matrix4x4 vp = proj * view;
            Shader.SetGlobalMatrix("_PointLightViewProjectionMatrix", vp);

            // Temporary 2D depth texture
            RenderTexture tmp = RenderTexture.GetTemporary(shadowResolution, shadowResolution, 0, RenderTextureFormat.RFloat);
            shadowCam.targetTexture = tmp;

            shadowCam.RenderWithShader(shadowCasterShader, "RenderType");
            shadowCam.targetTexture = null;

            // Copy into cubemap face
            Graphics.CopyTexture(tmp, 0, 0, shadowCubemap, face, 0);
            RenderTexture.ReleaseTemporary(tmp);
        }

        Shader.SetGlobalTexture("_PointLightShadowMap", shadowCubemap);
        Shader.SetGlobalFloat("_DepthBiasPointLight", depthBias);
    }

    Quaternion GetCubemapFaceRotation(int face)
    {
        switch (face)
        {
            case 0: // +X
                return Quaternion.LookRotation(Vector3.right, Vector3.up);
            case 1: // -X
                return Quaternion.LookRotation(Vector3.left, Vector3.up);
            case 2: // +Y
                return Quaternion.LookRotation(Vector3.up, Vector3.back);
            case 3: // -Y
                return Quaternion.LookRotation(Vector3.down, Vector3.forward);
            case 4: // +Z
                return Quaternion.LookRotation(Vector3.forward, Vector3.up);
            case 5: // -Z
                return Quaternion.LookRotation(Vector3.back, Vector3.up);
            default:
                return Quaternion.identity;
        }
    }

    void OnEnable() => Shader.SetGlobalFloat("_UsePointShadow", 1);
    void OnDisable() => Shader.SetGlobalFloat("_UsePointShadow", 0);
}
