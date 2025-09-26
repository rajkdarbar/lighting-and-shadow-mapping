using UnityEngine;

public class SpotLightShadowMap : MonoBehaviour
{
    public int sliceIndex = -1; // assigned by manager
    private Camera shadowCam;
    private Light spot;

    private static Shader shadowCasterShader;

    void Start()
    {
        spot = GetComponent<Light>();
        shadowCasterShader = Shader.Find("Custom/ShadowCasterSpotLight");

        // Shadow camera
        GameObject camObj = new GameObject("ShadowCam_" + gameObject.name);
        camObj.transform.SetParent(transform, false);
        shadowCam = camObj.AddComponent<Camera>();
        shadowCam.enabled = false;
        shadowCam.orthographic = false;
        shadowCam.clearFlags = CameraClearFlags.SolidColor;
        shadowCam.backgroundColor = SystemInfo.usesReversedZBuffer ? Color.black : Color.white;
    }

    public void RenderToSlice(RenderTexture shadowArray)
    {
        if (!spot || !spot.enabled || sliceIndex < 0) return;

        // Configure shadow camera
        shadowCam.transform.position = transform.position;
        shadowCam.transform.rotation = transform.rotation;
        shadowCam.fieldOfView = spot.spotAngle;
        shadowCam.aspect = 1.0f;
        shadowCam.nearClipPlane = 0.1f;
        shadowCam.farClipPlane = spot.range;

        // Allocate temporary depth RT
        RenderTexture tmp = new RenderTexture(
            shadowArray.width,
            shadowArray.height,
            24,
            RenderTextureFormat.Depth
        );
        tmp.dimension = UnityEngine.Rendering.TextureDimension.Tex2D;
        tmp.useMipMap = false;
        tmp.filterMode = FilterMode.Bilinear;
        tmp.wrapMode = TextureWrapMode.Clamp;
        tmp.Create();

        // Render depth
        shadowCam.targetTexture = tmp;
        shadowCam.RenderWithShader(shadowCasterShader, "RenderType");
        shadowCam.targetTexture = null;

        // Copy into slice of array
        Graphics.CopyTexture(tmp, 0, 0, shadowArray, sliceIndex, 0);

        tmp.Release();
    }

    public Matrix4x4 GetViewProjectionMatrix()
    {
        Matrix4x4 proj = GL.GetGPUProjectionMatrix(shadowCam.projectionMatrix, true);
        Matrix4x4 view = shadowCam.worldToCameraMatrix;
        return proj * view;
    }
}
