
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
        shadowCasterShader = Shader.Find("Custom/ShadowCaster");

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
        if (!spot || sliceIndex < 0) return;

        // Configure shadow camera
        shadowCam.transform.position = transform.position;
        shadowCam.transform.rotation = transform.rotation;
        shadowCam.fieldOfView = spot.spotAngle;
        shadowCam.aspect = 1.0f;
        shadowCam.nearClipPlane = 0.1f;
        shadowCam.farClipPlane = spot.range;

        // Temporary RT to render into
        RenderTexture tmp = RenderTexture.GetTemporary(
            shadowArray.width,
            shadowArray.height,
            16,
            shadowArray.format
        );

        shadowCam.targetTexture = tmp;
        shadowCam.RenderWithShader(shadowCasterShader, null);
        shadowCam.targetTexture = null;

        // Copy into the correct slice of the array
        Graphics.CopyTexture(tmp, 0, 0, shadowArray, sliceIndex, 0);

        RenderTexture.ReleaseTemporary(tmp);
    }

    public Matrix4x4 GetViewProjectionMatrix()
    {
        Matrix4x4 proj = GL.GetGPUProjectionMatrix(shadowCam.projectionMatrix, true);
        Matrix4x4 view = shadowCam.worldToCameraMatrix;
        return proj * view;
    }
}
