using UnityEditor;
using UnityEngine;
using static ZonaPruebasBuilder;

// Cielo nocturno con aurora (estilo Skyrim). Usa Assets/Texturas/Cielo.png (equirectangular 2:1).
// Para cambiarlo: reemplaza ese png por otro panorama 2:1 y corre "Construir TODO".
internal static class SkyKit
{
    internal static void Apply()
    {
        const string tp = "Assets/Texturas/Cielo.png";
        if (!System.IO.File.Exists(tp)) return;
        var ti = AssetImporter.GetAtPath(tp) as TextureImporter;
        if (ti != null && (ti.maxTextureSize != 2048 || ti.wrapMode != TextureWrapMode.Repeat || ti.mipmapEnabled))
        {
            ti.maxTextureSize = 2048;
            ti.wrapMode = TextureWrapMode.Repeat;
            ti.mipmapEnabled = false;
            ti.SaveAndReimport();
        }
        var tex = AssetDatabase.LoadAssetAtPath<Texture2D>(tp);
        var sh = Shader.Find("Skybox/Panoramic");
        if (tex == null || sh == null) return;

        string mp = "Assets/Materials/CieloNoche.mat";
        var m = AssetDatabase.LoadAssetAtPath<Material>(mp);
        if (m == null) { m = new Material(sh); AssetDatabase.CreateAsset(m, mp); }
        m.shader = sh;
        m.SetTexture("_MainTex", tex);
        m.SetFloat("_Mapping", 1f);       // latitud-longitud
        m.SetFloat("_ImageType", 0f);     // 360
        m.SetFloat("_Exposure", 1.15f);
        m.SetFloat("_Rotation", 200f);
        EditorUtility.SetDirty(m);

        RenderSettings.skybox = m;
        RenderSettings.fog = false;
        RenderSettings.ambientMode = UnityEngine.Rendering.AmbientMode.Flat;
        RenderSettings.ambientLight = new Color(0.42f, 0.47f, 0.6f);     // luz de luna fria
        var cam = Camera.main;
        if (cam != null) cam.clearFlags = CameraClearFlags.Skybox;
        foreach (var l in Object.FindObjectsByType<Light>(FindObjectsSortMode.None))
            if (l.type == LightType.Directional) { l.color = new Color(0.65f, 0.75f, 1f); l.intensity = 0.9f; }
    }
}
