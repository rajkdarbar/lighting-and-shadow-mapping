using UnityEngine;
using System.Collections.Generic;

public class CustomLightSender : MonoBehaviour
{
    void Update()
    {
        Light[] lights = GetComponentsInChildren<Light>();

        Light dir = null;
        Light point = null;
        Light spot = null;

        foreach (var l in lights)
        {
            if (!l.enabled) continue;

            if (l.type == LightType.Directional && dir == null)
                dir = l;

            else if (l.type == LightType.Spot && spot == null)
                spot = l;

            else if (l.type == LightType.Point && point == null)
                point = l;
        }

        // Directional light info
        if (dir)
        {
            Shader.SetGlobalColor("_DirectionalLightColor", dir.color * dir.intensity);
            Shader.SetGlobalVector("_DirectionalLightDir", dir.transform.forward);
            Shader.SetGlobalFloat("_DirectionalLightIntensity", dir.intensity);
        }

        // Spot light info
        if (spot)
        {
            Shader.SetGlobalVector("_SpotLightPos", spot.transform.position);
            Shader.SetGlobalVector("_SpotLightDir", spot.transform.forward.normalized);
            Shader.SetGlobalColor("_SpotLightColor", spot.color * spot.intensity);
            Shader.SetGlobalFloat("_SpotLightIntensity", spot.intensity);
            Shader.SetGlobalFloat("_SpotLightRange", spot.range);
            Shader.SetGlobalFloat("_SpotLightAngle", Mathf.Cos(0.5f * spot.spotAngle * Mathf.Deg2Rad));
        }

        // Point light info
        if (point)
        {
            Shader.SetGlobalVector("_PointLightPos", point.transform.position);
            Shader.SetGlobalColor("_PointLightColor", point.color);
            Shader.SetGlobalFloat("_PointLightIntensity", point.intensity);
            Shader.SetGlobalFloat("_PointLightRange", point.range);
        }
    }
}