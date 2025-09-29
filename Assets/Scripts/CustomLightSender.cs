
using UnityEngine;
using System.Collections.Generic;

public class CustomLightSender : MonoBehaviour
{
    void Update()
    {
        Light[] lights = GetComponentsInChildren<Light>();

        // --- Directional light ---
        bool dirFound = false;
        foreach (Light l in lights)
        {
            if (l.type == LightType.Directional && l.enabled)
            {
                Vector3 dir = l.transform.forward;
                Shader.SetGlobalColor("_DirectionalLightColor", l.color * l.intensity);
                Shader.SetGlobalVector("_DirectionalLightDir", dir); // negate in shade
                Shader.SetGlobalFloat("_DirectionalLightIntensity", l.intensity);
                dirFound = true;
                break; // only one
            }
        }

        if (!dirFound)
        {
            // reset if no directional light active
            Shader.SetGlobalColor("_DirectionalLightColor", Color.black);
            Shader.SetGlobalVector("_DirectionalLightDir", Vector3.zero);
            Shader.SetGlobalFloat("_DirectionalLightIntensity", 0f);
        }


        // --- Point lights ---
        List<Vector4> pointPositions = new List<Vector4>();
        List<Vector4> pointColors = new List<Vector4>();
        List<float> pointIntensities = new List<float>();
        List<float> pointRanges = new List<float>();

        // --- Spot lights ---
        List<Vector4> spotPositions = new List<Vector4>();
        List<Vector4> spotDirections = new List<Vector4>();
        List<Vector4> spotColors = new List<Vector4>();
        List<float> spotIntensities = new List<float>();
        List<float> spotRanges = new List<float>();
        List<float> spotAngles = new List<float>();

        foreach (Light l in lights)
        {
            if (!l.enabled) continue; // skip disabled lights

            if (l.type == LightType.Point)
            {
                pointPositions.Add(l.transform.position);
                pointColors.Add(l.color);
                pointIntensities.Add(l.intensity);
                pointRanges.Add(l.range);
            }
            else if (l.type == LightType.Spot)
            {
                spotPositions.Add(l.transform.position);
                spotDirections.Add(l.transform.forward); // negate in shader
                spotColors.Add(l.color);
                spotIntensities.Add(l.intensity);
                spotRanges.Add(l.range);
                spotAngles.Add(Mathf.Cos(l.spotAngle * Mathf.Deg2Rad));
            }
        }

        // Push point light data
        Shader.SetGlobalInt("_NumPointLights", pointPositions.Count);
        if (pointPositions.Count > 0)
        {
            Shader.SetGlobalVectorArray("_PointLightPos", pointPositions.ToArray());
            Shader.SetGlobalVectorArray("_PointLightColor", pointColors.ToArray());
            Shader.SetGlobalFloatArray("_PointLightIntensity", pointIntensities.ToArray());
            Shader.SetGlobalFloatArray("_PointLightRange", pointRanges.ToArray());
        }

        // Push spot light data
        Shader.SetGlobalInt("_NumSpotLights", spotPositions.Count);
        if (spotPositions.Count > 0)
        {
            Shader.SetGlobalVectorArray("_SpotLightPos", spotPositions.ToArray());
            Shader.SetGlobalVectorArray("_SpotLightDir", spotDirections.ToArray());
            Shader.SetGlobalVectorArray("_SpotLightColor", spotColors.ToArray());
            Shader.SetGlobalFloatArray("_SpotLightIntensity", spotIntensities.ToArray());
            Shader.SetGlobalFloatArray("_SpotLightRange", spotRanges.ToArray());
            Shader.SetGlobalFloatArray("_SpotLightAngle", spotAngles.ToArray());
        }
    }
}
