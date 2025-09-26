using UnityEngine;

[RequireComponent(typeof(Light))]
public class SingleSpotLightShadowMap : MonoBehaviour
{
    public int shadowResolution = 1024;

    private Camera shadowCam;
    public RenderTexture shadowMap;
    private Light spot;

    void Start()
    {
        spot = GetComponent<Light>();

        // Create RT
        //shadowMap = new RenderTexture(shadowResolution, shadowResolution, 16, RenderTextureFormat.RFloat);
        //shadowMap = new RenderTexture(shadowResolution, shadowResolution, 16, RenderTextureFormat.ARGB32);
        shadowMap = new RenderTexture(shadowResolution, shadowResolution, 24, RenderTextureFormat.Depth);


        shadowMap.useMipMap = false;
        shadowMap.filterMode = FilterMode.Bilinear;
        shadowMap.wrapMode = TextureWrapMode.Clamp;
        shadowMap.Create();

        // Camera
        GameObject camObj = new GameObject("SingleSpotShadowCam");
        camObj.transform.SetParent(transform, false);
        shadowCam = camObj.AddComponent<Camera>();
        shadowCam.enabled = false;
        shadowCam.orthographic = false;
        shadowCam.clearFlags = CameraClearFlags.SolidColor;
        shadowCam.backgroundColor = SystemInfo.usesReversedZBuffer ? Color.black : Color.white;
        shadowCam.targetTexture = shadowMap;

        // Debug quad
        var quad = GameObject.Find("QuadSpotLight");
        if (quad != null)
        {
            var m = new Material(Shader.Find("Custom/ShowSpotLightShadowMap"));
            m.SetTexture("_ShadowMap", shadowMap);
            quad.GetComponent<Renderer>().material = m;
        }
    }

    void LateUpdate()
    {
        if (!shadowCam || !spot) return;

        shadowCam.transform.position = transform.position;
        shadowCam.transform.rotation = transform.rotation;
        shadowCam.fieldOfView = spot.spotAngle;
        shadowCam.aspect = 1.0f;
        shadowCam.nearClipPlane = 0.1f;
        shadowCam.farClipPlane = spot.range;

        var scShader = Shader.Find("Custom/ShadowCasterSpotLight");
        if (scShader != null)
        {
            shadowCam.RenderWithShader(scShader, "RenderType");
        }
    }
}
