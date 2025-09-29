using UnityEngine;

public class PointLightShadowMap : MonoBehaviour
{
    private Light pointLight; // the point light
    private Camera shadowCam;
    private Matrix4x4[] vpMatrices = new Matrix4x4[6];

    public RenderTexture shadowCubemap;
    private static Shader shadowCasterShader;
    public int shadowResolution = 4096;

    void Start()
    {
        pointLight = GetComponent<Light>();
        shadowCasterShader = Shader.Find("Custom/ShadowCasterPointLight");

        // Create cubemap render texture
        shadowCubemap = new RenderTexture(shadowResolution, shadowResolution, 24, RenderTextureFormat.RFloat);
        shadowCubemap.dimension = UnityEngine.Rendering.TextureDimension.Cube;
        shadowCubemap.useMipMap = false;
        shadowCubemap.autoGenerateMips = false;
        shadowCubemap.Create();

        // Camera
        GameObject camObj = new GameObject("ShadowCam_" + gameObject.name);
        camObj.transform.SetParent(transform, false);
        shadowCam = camObj.AddComponent<Camera>();
        shadowCam.enabled = false;
        shadowCam.orthographic = false;
        shadowCam.clearFlags = CameraClearFlags.SolidColor;
        shadowCam.nearClipPlane = 0.1f;
        shadowCam.farClipPlane = pointLight.range;
        shadowCam.fieldOfView = 90f; // 90° per cube face
        shadowCam.aspect = 1f;

        // Debug quad
        var quad = GameObject.Find("QuadPointLight");
        if (quad != null)
        {
            var m = quad.GetComponent<Renderer>().material;
            m.SetTexture("_ShadowMap", shadowCubemap);
            quad.GetComponent<Renderer>().material = m;
        }

        Shader.SetGlobalFloat("_PointLightShadowMapSize", shadowResolution);

    }

    void Update()
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

            // Projection and view
            Matrix4x4 proj = Matrix4x4.Perspective(90f, 1f, shadowCam.nearClipPlane, shadowCam.farClipPlane);
            Matrix4x4 view = shadowCam.worldToCameraMatrix;
            vpMatrices[face] = proj * view;

            // Temporary 2D depth texture
            RenderTexture tmp = RenderTexture.GetTemporary(
                shadowResolution, shadowResolution, 24, RenderTextureFormat.RFloat
            );

            shadowCam.targetTexture = tmp;
            shadowCam.RenderWithShader(shadowCasterShader, "RenderType");
            shadowCam.targetTexture = null;

            // Copy into cubemap face
            Graphics.CopyTexture(tmp, 0, 0, shadowCubemap, face, 0);

            RenderTexture.ReleaseTemporary(tmp);
        }

        Shader.SetGlobalMatrixArray("_PointLightViewProjectionMatrix", vpMatrices);
        Shader.SetGlobalTexture("_PointLightShadowMap", shadowCubemap);
        Shader.SetGlobalVector("_PointLightPos0", transform.position);
        Shader.SetGlobalFloat("_PointLightRange0", pointLight.range);
    }

    Quaternion GetCubemapFaceRotation(int face)
    {
        switch (face)
        {
            case 0: // +X
                return Quaternion.LookRotation(Vector3.right, Vector3.down);
            case 1: // -X
                return Quaternion.LookRotation(Vector3.left, Vector3.down);
            case 2: // +Y
                return Quaternion.LookRotation(Vector3.up, Vector3.forward);
            case 3: // -Y
                return Quaternion.LookRotation(Vector3.down, Vector3.back);
            case 4: // +Z
                return Quaternion.LookRotation(Vector3.forward, Vector3.down);
            case 5: // -Z
                return Quaternion.LookRotation(Vector3.back, Vector3.down);
            default:
                return Quaternion.identity;
        }
    }
}
