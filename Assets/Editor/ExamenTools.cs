using System;
using System.IO;
using System.Reflection;
using System.Text;
using UnityEditor;
using UnityEditor.Build;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;

// Herramientas del EXAMEN (Meta Quest): crear el Hall, configurar Android/OpenXR para Quest y aplicar ajustes de rendimiento.
// Todo lo que toca XR/OpenXR va por reflexion: si un paquete cambia de nombre, solo se avisa en la consola (no rompe la compilacion).
public static class ExamenTools
{
    internal const string HallPath = "Assets/Scenes/Hall.unity";
    static readonly StringBuilder report = new StringBuilder();

    static void R(string s) { report.AppendLine(s); Debug.Log("[Examen] " + s); }

    static Type T(string name)
    {
        foreach (var a in AppDomain.CurrentDomain.GetAssemblies())
        {
            Type t = null;
            try { t = a.GetType(name, false); } catch { }
            if (t != null) return t;
        }
        return null;
    }

    // ------------------------------------------------------------------ 1. Hall
    [MenuItem("Examen/1 - Crear escena Hall (hall de acceso)")]
    static void CreateHall()
    {
        if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;
        if (!AssetDatabase.IsValidFolder("Assets/Scenes")) AssetDatabase.CreateFolder("Assets", "Scenes");
        var scene = EditorSceneManager.NewScene(NewSceneSetup.DefaultGameObjects, NewSceneMode.Single);
        new GameObject("HallController").AddComponent<HallController>();
        EditorSceneManager.MarkSceneDirty(scene);
        EditorSceneManager.SaveScene(scene, HallPath);
        ZonaPruebasBuilder.UpdateBuildSettings();
        AssetDatabase.SaveAssets();
        Debug.Log("Hall creado en " + HallPath + " (queda primero en Build Profiles).");
        EditorUtility.DisplayDialog("Hall", "Escena Hall creada y puesta como PRIMERA escena del build.\n\nAbrela y dale Play: camina con WASD, clic derecho + mouse para mirar, y cruza el portal.", "OK");
    }

    // ------------------------------------------------------------------ 2. Quest
    [MenuItem("Examen/2 - Configurar Meta Quest (Android + OpenXR)")]
    static void ConfigQuest()
    {
        report.Clear();
        R("=== Configuracion Meta Quest " + DateTime.Now.ToString("yyyy-MM-dd HH:mm") + " ===");
        try
        {
            if (EditorUserBuildSettings.activeBuildTarget != BuildTarget.Android)
            {
                R("Cambiando plataforma de compilacion a Android (puede tardar: reimporta assets)...");
                EditorUserBuildSettings.SwitchActiveBuildTarget(NamedBuildTarget.Android, BuildTarget.Android);
            }
            else R("Plataforma de compilacion: Android (ya estaba).");

            PlayerSettings.SetScriptingBackend(NamedBuildTarget.Android, ScriptingImplementation.IL2CPP);
            PlayerSettings.Android.targetArchitectures = AndroidArchitecture.ARM64;
            PlayerSettings.Android.minSdkVersion = AndroidSdkVersions.AndroidApiLevel29;
            PlayerSettings.colorSpace = ColorSpace.Linear;
            PlayerSettings.defaultInterfaceOrientation = UIOrientation.LandscapeLeft;
            R("Player: IL2CPP, ARM64, minSdk 29 (Android 10), espacio de color Linear.");
        }
        catch (Exception ex) { R("ERROR Player/plataforma: " + ex.Message); }

        // cargador XR: OpenXR en Android
        try
        {
            var tPer = T("UnityEditor.XR.Management.XRGeneralSettingsPerBuildTarget");
            var tStore = T("UnityEditor.XR.Management.Metadata.XRPackageMetadataStore");
            if (tPer == null || tStore == null) R("AVISO: no encuentro XR Plug-in Management (instalalo en Package Manager).");
            else
            {
                var gs = tPer.GetMethod("XRGeneralSettingsForBuildTarget", BindingFlags.Public | BindingFlags.Static).Invoke(null, new object[] { BuildTargetGroup.Android });
                if (gs == null) R("AVISO: abre Project Settings > XR Plug-in Management > Android una vez y vuelve a ejecutar esto.");
                else
                {
                    var mgr = gs.GetType().GetProperty("AssignedSettings").GetValue(gs);
                    var assign = tStore.GetMethod("AssignLoader", BindingFlags.Public | BindingFlags.Static);
                    object ok = assign.Invoke(null, new object[] { mgr, "UnityEngine.XR.OpenXR.OpenXRLoader", BuildTargetGroup.Android });
                    var init = gs.GetType().GetProperty("InitManagerOnStart");
                    if (init != null) init.SetValue(gs, true);
                    EditorUtility.SetDirty((UnityEngine.Object)gs);
                    R("XR Plug-in Management (Android): loader OpenXR asignado = " + ok + ", inicializar al arrancar = si.");
                }
            }
        }
        catch (Exception ex) { R("ERROR XR loader: " + (ex.InnerException != null ? ex.InnerException.Message : ex.Message)); }

        // OpenXR: perfil de controles Touch + Meta Quest + render single pass
        try
        {
            var tSet = T("UnityEngine.XR.OpenXR.OpenXRSettings");
            if (tSet == null) R("AVISO: no encuentro OpenXR (instalalo en Package Manager).");
            else
            {
                var s = tSet.GetMethod("GetSettingsForBuildTargetGroup", BindingFlags.Public | BindingFlags.Static).Invoke(null, new object[] { BuildTargetGroup.Android });
                if (s == null) R("AVISO: OpenXR no tiene ajustes para Android todavia.");
                else
                {
                    var feats = (Array)tSet.GetMethod("GetFeatures", Type.EmptyTypes).Invoke(s, null);
                    int on = 0;
                    foreach (object f in feats)
                    {
                        string n = f.GetType().Name;
                        if (n.Contains("OculusTouchControllerProfile") || n.Contains("MetaQuestFeature") || n.Contains("MetaQuestTouchProControllerProfile") || n.Contains("MetaQuestTouchPlusControllerProfile"))
                        {
                            f.GetType().GetProperty("enabled").SetValue(f, true);
                            EditorUtility.SetDirty((UnityEngine.Object)f);
                            R("OpenXR feature activada: " + n);
                            on++;
                        }
                    }
                    if (on == 0) R("AVISO: no encontre features de Meta Quest; marcalas a mano en OpenXR > Android.");
                    var rm = tSet.GetProperty("renderMode");
                    if (rm != null)
                    {
                        rm.SetValue(s, Enum.Parse(rm.PropertyType, "SinglePassInstanced"));
                        R("OpenXR render mode = Single Pass Instanced (menos draw calls: un dibujo para los dos ojos).");
                    }
                    EditorUtility.SetDirty((UnityEngine.Object)s);
                }
            }
        }
        catch (Exception ex) { R("ERROR OpenXR: " + (ex.InnerException != null ? ex.InnerException.Message : ex.Message)); }

        // escenas en el build (Hall primero)
        ZonaPruebasBuilder.UpdateBuildSettings();
        R("Escenas del build (orden): " + string.Join(", ", Array.ConvertAll(EditorBuildSettings.scenes, x => Path.GetFileNameWithoutExtension(x.path))));
        AssetDatabase.SaveAssets();
        R("FALTA A MANO (si no esta): Player > Other Settings > Active Input Handling = Input System Package (New) y reiniciar Unity.");
        R("FALTA A MANO: en Quest activar modo desarrollador y conectar por USB (adb devices) antes de Build And Run.");
        WriteReport("configuracion_quest.txt");
        EditorUtility.DisplayDialog("Quest", report.ToString(), "OK");
    }

    // ------------------------------------------------------------------ 3. Rendimiento
    // Ajusta el/los URP Asset del proyecto con valores pensados para el Quest y guarda un ANTES/DESPUES en Informe/.
    [MenuItem("Examen/3 - Aplicar ajustes de rendimiento Quest (URP) con registro antes-despues")]
    static void ApplyPerf()
    {
        report.Clear();
        R("=== Ajustes de rendimiento URP " + DateTime.Now.ToString("yyyy-MM-dd HH:mm") + " ===");
        var done = new System.Collections.Generic.HashSet<UnityEngine.Object>();
        var list = new System.Collections.Generic.List<RenderPipelineAsset>();
        if (GraphicsSettings.defaultRenderPipeline != null) list.Add(GraphicsSettings.defaultRenderPipeline);
        for (int i = 0; i < QualitySettings.count; i++)
        {
            var a = QualitySettings.GetRenderPipelineAssetAt(i);
            if (a != null) list.Add(a);
        }
        foreach (var a in list)
        {
            if (!done.Add(a)) continue;
            var so = new SerializedObject(a);
            R("-- " + a.name);
            Set(so, "m_RenderScale", 0.9f);                       // resolucion de render: 90 %
            Set(so, "m_SupportsHDR", false);                      // sin HDR (caro en movil)
            Set(so, "m_MSAA", 4);                                 // MSAA 4x (lo recomendado en Quest; mejor que post-proceso)
            Set(so, "m_ShadowDistance", 25f);                     // sombras solo cerca
            Set(so, "m_SupportsCameraDepthTexture", false);       // sin textura de profundidad extra
            Set(so, "m_SupportsCameraOpaqueTexture", false);      // sin copia de color opaco
            Set(so, "m_AdditionalLightsPerObjectLimit", 2);       // maximo 2 luces extra por objeto
            Set(so, "m_MainLightShadowmapResolution", 1024);      // sombra de la luz principal mas chica
            so.ApplyModifiedProperties();
            EditorUtility.SetDirty(a);
        }
        AssetDatabase.SaveAssets();
        R("Quita tambien: Post Processing en la camara (si lo hay) y Anti-aliasing de camara distinto de MSAA.");
        WriteReport("ajustes_rendimiento_antes_despues.txt");
        EditorUtility.DisplayDialog("Rendimiento", report.ToString(), "OK");
    }

    static void Set(SerializedObject so, string prop, object val)
    {
        var p = so.FindProperty(prop);
        if (p == null) { R("   (no existe " + prop + " en esta version de URP, se omite)"); return; }
        string before;
        switch (p.propertyType)
        {
            case SerializedPropertyType.Float: before = p.floatValue.ToString("0.##"); p.floatValue = Convert.ToSingle(val); break;
            case SerializedPropertyType.Boolean: before = p.boolValue.ToString(); p.boolValue = Convert.ToBoolean(val); break;
            case SerializedPropertyType.Integer:
            case SerializedPropertyType.Enum: before = p.intValue.ToString(); p.intValue = Convert.ToInt32(val); break;
            default: R("   (tipo no soportado: " + prop + ")"); return;
        }
        R("   " + prop + ": " + before + " -> " + val);
    }

    static void WriteReport(string file)
    {
        try
        {
            string dir = Path.Combine(Directory.GetParent(Application.dataPath).FullName, "Informe");
            Directory.CreateDirectory(dir);
            File.WriteAllText(Path.Combine(dir, file), report.ToString());
            Debug.Log("[Examen] Registro guardado en " + Path.Combine(dir, file));
        }
        catch (Exception ex) { Debug.LogWarning("[Examen] No pude guardar el registro: " + ex.Message); }
    }
}
