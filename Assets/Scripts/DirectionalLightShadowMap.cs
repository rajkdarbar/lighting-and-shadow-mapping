using UnityEngine;

[RequireComponent(typeof(Light))]
public class DirectionalLightShadowMap : MonoBehaviour
{
    public int shadowResolution = 1024;
    public float orthoSize = 10f;
    public float nearPlane = 0.1f;
    public float farPlane = 50f;

    private Camera shadowCam;
    private RenderTexture shadowMap;

    private Light dirLight;

    void Start()
    {
        dirLight = GetComponent<Light>();

        // Create shadow map render texture
        shadowMap = new RenderTexture(shadowResolution, shadowResolution, 16, RenderTextureFormat.Shadowmap);
        shadowMap.wrapMode = TextureWrapMode.Clamp;
        shadowMap.filterMode = FilterMode.Bilinear;

        // Create hidden camera
        GameObject camObj = new GameObject("DirectionalShadowCam");
        camObj.hideFlags = HideFlags.HideAndDontSave;
        shadowCam = camObj.AddComponent<Camera>();

        shadowCam.enabled = false;
        shadowCam.orthographic = true;
        shadowCam.orthographicSize = orthoSize;
        shadowCam.nearClipPlane = nearPlane;
        shadowCam.farClipPlane = farPlane;
        shadowCam.clearFlags = CameraClearFlags.Depth;
        shadowCam.backgroundColor = Color.white;
        shadowCam.targetTexture = shadowMap;

        // Assign to shader
        Shader.SetGlobalTexture("_DirectionalShadowMap", shadowMap);
    }

    void LateUpdate()
    {
        // Align shadow camera with light
        shadowCam.transform.position = transform.position;
        shadowCam.transform.rotation = transform.rotation;

        // Render depth
        shadowCam.RenderWithShader(Shader.Find("Custom/ShadowCaster"), "RenderType");

        // Build light-space matrix (bias to [0,1])
        Matrix4x4 view = shadowCam.worldToCameraMatrix;
        Matrix4x4 proj = GL.GetGPUProjectionMatrix(shadowCam.projectionMatrix, false);

        Matrix4x4 vp = proj * view;

        // Bias matrix for texture coords (from -1..1 to 0..1)
        Matrix4x4 bias = Matrix4x4.identity;
        bias.m00 = bias.m11 = bias.m22 = 0.5f;
        bias.m03 = bias.m13 = bias.m23 = 0.5f;

        Matrix4x4 lightSpaceMatrix = bias * vp;

        Shader.SetGlobalMatrix("_DirectionalLightSpaceMatrix", lightSpaceMatrix);
    }
}
