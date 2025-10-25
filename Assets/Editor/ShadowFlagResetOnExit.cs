#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;

[InitializeOnLoad]
public static class ShadowFlagResetOnExit
{
    static ShadowFlagResetOnExit()
    {
        EditorApplication.playModeStateChanged += (state) =>
        {
            if (state == PlayModeStateChange.EnteredEditMode)
            {
                Shader.SetGlobalFloat("_UseDirShadow", 0);
                Shader.SetGlobalFloat("_UseSpotShadow", 0);
                Shader.SetGlobalFloat("_UsePointShadow", 0);
            }
        };
    }
}
#endif
