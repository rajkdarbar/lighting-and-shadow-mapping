using UnityEngine;
using System.Collections.Generic;

public class SpotLightShadowManager : MonoBehaviour
{
    public int shadowResolution = 4096;
    public RenderTexture shadowArray;

    private SpotLightShadowMap[] spotShadows;
    private Dictionary<int, int> sliceMap = new Dictionary<int, int>();
    private int count = 0;

    [Range(0, 3)]
    public int debugSlice = 0; // inspector slider

    void Start()
    {
        // Find all spotlight shadow components
        spotShadows = GetComponentsInChildren<SpotLightShadowMap>(true);
        count = spotShadows.Length;

        // Allocate 2D texture array for all potential spotlights
        shadowArray = new RenderTexture(shadowResolution, shadowResolution, 24, RenderTextureFormat.Depth);
        shadowArray.dimension = UnityEngine.Rendering.TextureDimension.Tex2DArray;
        shadowArray.volumeDepth = Mathf.Max(1, count);
        shadowArray.useMipMap = false;
        shadowArray.filterMode = FilterMode.Bilinear;
        shadowArray.wrapMode = TextureWrapMode.Clamp;
        shadowArray.Create();

        // Global for shaders
        Shader.SetGlobalFloat("_SpotLightShadowMapSize", shadowResolution);
        Shader.SetGlobalTexture("_SpotLightShadowMaps", shadowArray);

        // Debug quad
        var quad = GameObject.Find("QuadSpotLight");
        if (quad != null)
        {
            var m = new Material(Shader.Find("Custom/ShowSpotLightShadowMapArray"));
            m.SetTexture("_ShadowMaps", shadowArray);
            quad.GetComponent<Renderer>().material = m;
        }
    }

    void LateUpdate()
    {
        List<Matrix4x4> matrices = new List<Matrix4x4>();
        int nextSlice = 0;

        foreach (var spot in spotShadows)
        {
            // Skip if disabled or not active
            if (spot == null || !spot.isActiveAndEnabled) continue;
            Light l = spot.GetComponent<Light>();
            if (!l || !l.enabled) continue;

            int id = spot.GetInstanceID();

            // Stable slice index
            if (!sliceMap.ContainsKey(id))
                sliceMap[id] = nextSlice++;

            int slice = sliceMap[id];

            // Render spotlight into slice
            spot.sliceIndex = slice;
            spot.RenderToSlice(shadowArray);

            // Store VP matrix
            matrices.Add(spot.GetViewProjectionMatrix());
        }

        int activeCount = matrices.Count;
        if (activeCount > 0)
        {
            // Global for shaders
            Shader.SetGlobalMatrixArray("_SpotLightViewProjectionMatrix", matrices.ToArray());
        }

        // Debug view
        int maxSlice = Mathf.Max(0, shadowArray.volumeDepth - 1);
        Shader.SetGlobalInt("_Slice", Mathf.Clamp(debugSlice, 0, maxSlice));
    }
}
