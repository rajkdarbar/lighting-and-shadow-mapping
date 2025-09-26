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
        // Count spotlights dynamically
        spotShadows = GetComponentsInChildren<SpotLightShadowMap>();
        count = spotShadows.Length;

        // Allocate 2D texture array for all spotlights
        shadowArray = new RenderTexture(shadowResolution, shadowResolution, 16, RenderTextureFormat.RFloat);
        shadowArray.dimension = UnityEngine.Rendering.TextureDimension.Tex2DArray;
        shadowArray.volumeDepth = Mathf.Max(1, count);
        shadowArray.useMipMap = false;
        shadowArray.filterMode = FilterMode.Bilinear;
        shadowArray.wrapMode = TextureWrapMode.Clamp;
        shadowArray.Create();

        // Global for shaders
        Shader.SetGlobalTexture("_SpotLightShadowMaps", shadowArray);

        var quad = GameObject.Find("QuadSpotLight");
        if (quad != null)
        {
            var m = new Material(Shader.Find("Custom/ShowSpotLightShadowMap"));
            m.SetTexture("_ShadowMaps", shadowArray); // or "_SpotLightShadowMaps" depending on your shader
            quad.GetComponent<Renderer>().material = m;
        }
    }

    void LateUpdate()
    {
        Matrix4x4[] matrices = new Matrix4x4[count];
        int nextSlice = 0;

        foreach (var spot in spotShadows)
        {
            int id = spot.GetInstanceID();

            // Stable slice index
            if (!sliceMap.ContainsKey(id))
                sliceMap[id] = nextSlice++;

            int slice = sliceMap[id];

            // Render spotlight into slice
            spot.sliceIndex = slice;
            spot.RenderToSlice(shadowArray);

            // Store VP matrix
            matrices[slice] = spot.GetViewProjectionMatrix();
        }

        // Global for shaders
        Shader.SetGlobalInt("_NumSpotLights", count);
        Shader.SetGlobalMatrixArray("_SpotLightViewProjectionMatrix", matrices);

        // Debug view: just pass array + slice to material (already on Quad)
        int maxSlice = Mathf.Max(0, shadowArray.volumeDepth - 1);
        Shader.SetGlobalInt("_Slice", Mathf.Clamp(debugSlice, 0, maxSlice));
    }
}
