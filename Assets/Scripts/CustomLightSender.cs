
using UnityEngine;
using System.Collections.Generic;

public class CustomLightSender : MonoBehaviour
{
    // limits must match shader constants
    const int MAX_POINT_LIGHTS = 3;
    const int MAX_SPOT_LIGHTS = 2;

    void Update()
    {
        Light[] lights = GetComponentsInChildren<Light>();

        // Directional
        foreach (Light l in lights)
        {
            if (l.type == LightType.Directional)
            {
                Vector3 dir = l.transform.forward;
                Shader.SetGlobalColor("_DirectionalLightColor", l.color);
                Shader.SetGlobalVector("_DirectionalLightDir", dir); // negate in shader
                Shader.SetGlobalFloat("_DirectionalLightIntensity", l.intensity);
                break; // only one
            }
        }

        // Arrays for point lights
        Vector4[] pointPositions = new Vector4[MAX_POINT_LIGHTS];
        Vector4[] pointColors = new Vector4[MAX_POINT_LIGHTS];
        float[] pointIntensities = new float[MAX_POINT_LIGHTS];
        float[] pointRanges = new float[MAX_POINT_LIGHTS];

        int pIndex = 0;

        // Arrays for spot lights
        Vector4[] spotPositions = new Vector4[MAX_SPOT_LIGHTS];
        Vector4[] spotDirections = new Vector4[MAX_SPOT_LIGHTS];
        Vector4[] spotColors = new Vector4[MAX_SPOT_LIGHTS];
        float[] spotIntensities = new float[MAX_SPOT_LIGHTS];
        float[] spotRanges = new float[MAX_SPOT_LIGHTS];
        float[] spotAngles = new float[MAX_SPOT_LIGHTS];

        int sIndex = 0;

        foreach (Light l in lights)
        {
            if (l.type == LightType.Point && pIndex < MAX_POINT_LIGHTS)
            {
                pointPositions[pIndex] = l.transform.position;
                pointColors[pIndex] = l.color;
                pointIntensities[pIndex] = l.intensity;
                pointRanges[pIndex] = l.range;
                pIndex++;
            }
            else if (l.type == LightType.Spot && sIndex < MAX_SPOT_LIGHTS)
            {
                spotPositions[sIndex] = l.transform.position;
                spotDirections[sIndex] = l.transform.forward; // negate in shader
                spotColors[sIndex] = l.color;
                spotIntensities[sIndex] = l.intensity;
                spotRanges[sIndex] = l.range;
                spotAngles[sIndex] = Mathf.Cos(l.spotAngle * Mathf.Deg2Rad);
                sIndex++;
            }
        }

        // Push to shader
        Shader.SetGlobalInt("_NumPointLights", pIndex);
        Shader.SetGlobalVectorArray("_PointLightPos", pointPositions);
        Shader.SetGlobalVectorArray("_PointLightColor", pointColors);
        Shader.SetGlobalFloatArray("_PointLightIntensity", pointIntensities);
        Shader.SetGlobalFloatArray("_PointLightRange", pointRanges);

        Shader.SetGlobalInt("_NumSpotLights", sIndex);
        Shader.SetGlobalVectorArray("_SpotLightPos", spotPositions);
        Shader.SetGlobalVectorArray("_SpotLightDir", spotDirections);
        Shader.SetGlobalVectorArray("_SpotLightColor", spotColors);
        Shader.SetGlobalFloatArray("_SpotLightIntensity", spotIntensities);
        Shader.SetGlobalFloatArray("_SpotLightRange", spotRanges);
        Shader.SetGlobalFloatArray("_SpotLightAngle", spotAngles);
    }
}
