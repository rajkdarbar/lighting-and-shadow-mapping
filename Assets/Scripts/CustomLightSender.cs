
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
        Light spot = null;

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
                spot = l;
                break; // use the first enabled spotlight
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

        // Push spotlight data
        if (spot != null)
        {
            Shader.SetGlobalVector("_SpotLightPos", spot.transform.position);
            Shader.SetGlobalVector("_SpotLightDir", spot.transform.forward.normalized);
            Shader.SetGlobalColor("_SpotLightColor", spot.color * spot.intensity);
            Shader.SetGlobalFloat("_SpotLightIntensity", spot.intensity);
            Shader.SetGlobalFloat("_SpotLightRange", spot.range);
            Shader.SetGlobalFloat("_SpotLightAngle", Mathf.Cos(0.5f * spot.spotAngle * Mathf.Deg2Rad));
            Shader.SetGlobalInt("_NumSpotLights", 1);
        }
        else
        {
            Shader.SetGlobalInt("_NumSpotLights", 0);
        }
    }
}