
using UnityEngine;

public class GlobalAmbientController : MonoBehaviour
{
    public float ambient = 0.05f;

    void Start()
    {
        var renderers = Object.FindObjectsByType<Renderer>(FindObjectsSortMode.None);
        foreach (var r in renderers)
        {
            var mats = r.sharedMaterials;
            for (int i = 0; i < mats.Length; i++)
            {
                var m = mats[i];
                if (m && m.shader && m.shader.name == "Custom/BlinnPhong")
                    m.SetFloat("_AmbientIntensity", ambient);
            }
        }
    }
}
