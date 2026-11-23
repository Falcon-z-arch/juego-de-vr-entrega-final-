using System.Collections.Generic;
using System.IO;
using System.Text;
using TMPro;
using Unity.Profiling;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;
using UnityEngine.XR;

// Herramienta de MEDICION y de OPTIMIZACION para el analisis de eficiencia del examen.
//  - Mide FPS, tiempo de cuadro (promedio, peor, percentil 99), draw calls / set-pass / triangulos (solo "Development Build" o editor),
//    cantidad de renderers y luces, memoria y la escena actual. Escribe un CSV y un resumen en la carpeta de datos de la app.
//  - Muestra un panel pequeno pegado a la camara (sirve dentro del lente).
//  - QuestPerf.Apply(true/false) enciende/apaga las optimizaciones para poder comparar ANTES y DESPUES con el mismo recorrido.
// Teclas: P = panel, O = optimizaciones. En el Quest: ambos clics de joystick = panel; X + Y juntos = optimizaciones.
public class PerfProbe : MonoBehaviour
{
    static PerfProbe inst;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    static void ResetStatics() { inst = null; }

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    static void Boot()
    {
        if (inst != null) return;
        var g = new GameObject("PerfProbe");
        DontDestroyOnLoad(g);
        inst = g.AddComponent<PerfProbe>();
    }

    ProfilerRecorder rDraw, rSet, rTris, rBatch;
    InputAction kP, kO, lClick, rClick, bX, bY;
    TextMeshPro label;
    Camera labelCam;
    int mode;            // 0 = solo FPS (por defecto), 1 = panel completo, 2 = oculto
    float timer, comboT;
    int frames; float sumDt, worst;
    readonly int[] hist = new int[400];      // 0.25 ms por casilla
    long totalFrames; double totalDt;
    float lastFps, lastMs;
    string csvPath, sumPath;
    string tag = "";
    int rendCount, lightCount; float cacheT;

    void OnEnable()
    {
        rDraw = ProfilerRecorder.StartNew(ProfilerCategory.Render, "Draw Calls Count");
        rSet = ProfilerRecorder.StartNew(ProfilerCategory.Render, "SetPass Calls Count");
        rTris = ProfilerRecorder.StartNew(ProfilerCategory.Render, "Triangles Count");
        rBatch = ProfilerRecorder.StartNew(ProfilerCategory.Render, "Batches Count");
        kP = new InputAction("p", InputActionType.Button, "<Keyboard>/p");
        kO = new InputAction("o", InputActionType.Button, "<Keyboard>/o");
        lClick = new InputAction("lc", InputActionType.Button, "<XRController>{LeftHand}/thumbstickClicked");
        rClick = new InputAction("rc", InputActionType.Button, "<XRController>{RightHand}/thumbstickClicked");
        bX = new InputAction("x", InputActionType.Button, "<XRController>{LeftHand}/primaryButton");
        bY = new InputAction("y", InputActionType.Button, "<XRController>{LeftHand}/secondaryButton");
        foreach (var a in new[] { kP, kO, lClick, rClick, bX, bY }) a.Enable();
        SceneManager.sceneLoaded += OnScene;
        string dir = Application.persistentDataPath;
        csvPath = Path.Combine(dir, "perf_log.csv");
        sumPath = Path.Combine(dir, "perf_resumen.txt");
        try { if (!File.Exists(csvPath)) File.WriteAllText(csvPath, "tiempo_s,escena,optimizado,fps,ms_prom,ms_peor,drawcalls,setpass,triangulos,renderers,luces,mem_mb,vr\n"); } catch { }
        QuestPerf.Apply(PlayerPrefs.GetInt("optimizar", 1) == 1);
    }

    void OnDisable()
    {
        rDraw.Dispose(); rSet.Dispose(); rTris.Dispose(); rBatch.Dispose();
        SceneManager.sceneLoaded -= OnScene;
    }

    void OnScene(Scene s, LoadSceneMode m)
    {
        QuestPerf.Apply(PlayerPrefs.GetInt("optimizar", 1) == 1);   // las luces nuevas de la escena tambien se optimizan
        cacheT = 0f;
    }

    void Update()
    {
        float dt = Time.unscaledDeltaTime;
        if (dt <= 0f) return;
        frames++; sumDt += dt; if (dt > worst) worst = dt;
        totalFrames++; totalDt += dt;
        hist[Mathf.Clamp((int)(dt * 4000f), 0, hist.Length - 1)]++;

        // entradas
        if (kP.WasPressedThisFrame() || (lClick.IsPressed() && rClick.IsPressed() && (lClick.WasPressedThisFrame() || rClick.WasPressedThisFrame()))) { mode = (mode + 1) % 3; UpdateLabel(); }
        bool both = bX.IsPressed() && bY.IsPressed();
        comboT = both ? comboT + dt : 0f;
        if (kO.WasPressedThisFrame() || (both && comboT > 0.8f && comboT - dt <= 0.8f)) ToggleOpt();

        timer += dt;
        if (timer >= 1f)
        {
            lastFps = frames / sumDt; lastMs = sumDt / frames * 1000f;
            Log(worst * 1000f);
            frames = 0; sumDt = 0f; worst = 0f; timer = 0f;
            UpdateLabel();
            if (((int)Time.unscaledTime) % 10 == 0) WriteSummary();
        }
        if (mode != 2 && XRSettings.isDeviceActive) EnsureLabel();
        else if (label != null) label.gameObject.SetActive(false);
    }

    void ToggleOpt()
    {
        bool on = PlayerPrefs.GetInt("optimizar", 1) != 1;
        PlayerPrefs.SetInt("optimizar", on ? 1 : 0); PlayerPrefs.Save();
        QuestPerf.Apply(on);
        totalFrames = 0; totalDt = 0; System.Array.Clear(hist, 0, hist.Length);
        HUD.Say("Optimizaciones: " + (on ? "ACTIVADAS" : "DESACTIVADAS") + " (medicion reiniciada)");
        Sfx.Beep(on ? 880f : 330f, 0.15f);
    }

    int Draw => rDraw.Valid ? (int)rDraw.LastValue : -1;
    int SetP => rSet.Valid ? (int)rSet.LastValue : -1;
    long Tris => rTris.Valid ? rTris.LastValue : -1;

    void CountObjects()
    {
        if (Time.unscaledTime < cacheT) return;
        cacheT = Time.unscaledTime + 3f;
        if (PlayerPrefs.GetInt("optimizar", 1) == 1) QuestPerf.Lights();
        rendCount = 0;
        foreach (var r in FindObjectsByType<Renderer>(FindObjectsSortMode.None)) if (r.enabled && r.gameObject.activeInHierarchy) rendCount++;
        lightCount = 0;
        foreach (var l in FindObjectsByType<Light>(FindObjectsSortMode.None)) if (l.enabled && l.gameObject.activeInHierarchy) lightCount++;
    }

    float P99()
    {
        long total = 0; foreach (var c in hist) total += c;
        if (total == 0) return 0f;
        long target = (long)(total * 0.99), acc = 0;
        for (int i = 0; i < hist.Length; i++) { acc += hist[i]; if (acc >= target) return (i + 1) * 0.25f; }
        return hist.Length * 0.25f;
    }

    void Log(float worstMs)
    {
        CountObjects();
        float mem = (float)(UnityEngine.Profiling.Profiler.GetTotalAllocatedMemoryLong() / (1024.0 * 1024.0));
        try
        {
            File.AppendAllText(csvPath, string.Format(System.Globalization.CultureInfo.InvariantCulture,
                "{0:F0},{1},{2},{3:F1},{4:F2},{5:F2},{6},{7},{8},{9},{10},{11:F0},{12}\n",
                Time.unscaledTime, SceneManager.GetActiveScene().name, PlayerPrefs.GetInt("optimizar", 1), lastFps, lastMs, worstMs, Draw, SetP, Tris, rendCount, lightCount, mem, XRSettings.isDeviceActive ? 1 : 0));
        }
        catch { }
    }

    void WriteSummary()
    {
        if (totalFrames < 10) return;
        double avgMs = totalDt / totalFrames * 1000.0;
        var sb = new StringBuilder();
        sb.AppendLine("ASCUA - resumen de rendimiento");
        sb.AppendLine("Dispositivo: " + SystemInfo.deviceModel + " | GPU: " + SystemInfo.graphicsDeviceName + " | API: " + SystemInfo.graphicsDeviceType);
        sb.AppendLine("Escena: " + SceneManager.GetActiveScene().name + " | VR activo: " + XRSettings.isDeviceActive + " | Optimizaciones: " + (PlayerPrefs.GetInt("optimizar", 1) == 1 ? "ON" : "OFF"));
        sb.AppendLine(string.Format("Muestra: {0} cuadros ({1:F0} s)", totalFrames, totalDt));
        sb.AppendLine(string.Format("FPS promedio: {0:F1} | tiempo de cuadro prom: {1:F2} ms | percentil 99: {2:F2} ms (1% peor = {3:F1} FPS)", 1000.0 / avgMs, avgMs, P99(), 1000f / Mathf.Max(0.01f, P99())));
        sb.AppendLine("Draw calls: " + Draw + " | SetPass: " + SetP + " | Triangulos: " + Tris + "  (-1 = no disponible: compilar con Development Build)");
        sb.AppendLine("Renderers activos: " + rendCount + " | Luces activas: " + lightCount + " | Memoria: " + (UnityEngine.Profiling.Profiler.GetTotalAllocatedMemoryLong() / 1048576) + " MB");
        try { File.WriteAllText(sumPath, sb.ToString()); } catch { }
    }

    // ---- panel pegado a la camara (visible en el lente)
    void EnsureLabel()
    {
        var cam = Camera.main;
        if (cam == null) return;
        if (label == null || labelCam != cam)
        {
            if (label != null) Destroy(label.gameObject);
            var g = new GameObject("PerfPanel");
            g.transform.SetParent(cam.transform, false);
            g.transform.localPosition = new Vector3(-0.32f, 0.2f, 1.0f);
            g.transform.localScale = Vector3.one * 0.1f;
            label = g.AddComponent<TextMeshPro>();
            label.fontSize = 1.6f; label.color = new Color(0.6f, 1f, 0.7f);
            label.alignment = TextAlignmentOptions.TopLeft;
            label.rectTransform.sizeDelta = new Vector2(6f, 3f);
            label.textWrappingMode = TextWrappingModes.NoWrap;
            labelCam = cam;
        }
        label.gameObject.SetActive(true);
    }

    void UpdateLabel()
    {
        if (label == null) return;
        label.text = Text();
    }

    string Text()
    {
        if (mode == 0) return string.Format("FPS {0:F0}", lastFps);
        double avgMs = totalFrames > 0 ? totalDt / totalFrames * 1000.0 : 0;
        return string.Format("FPS {0:F0}  ({1:F1} ms)\nP99 {2:F1} ms | prom {3:F1} FPS\nDraw {4} | SetPass {5} | Tris {6}\nRenderers {7} | Luces {8}\nOptimizaciones: {9}",
            lastFps, lastMs, P99(), avgMs > 0 ? 1000.0 / avgMs : 0, Draw, SetP, Tris, rendCount, lightCount, PlayerPrefs.GetInt("optimizar", 1) == 1 ? "ON" : "OFF");
    }

    // contador de FPS en pantalla (celular / PC); en el lente se usa el panel pegado a la camara
    GUIStyle st;
    void OnGUI()
    {
        if (mode == 2 || XRSettings.isDeviceActive) return;
        if (st == null) st = new GUIStyle(GUI.skin.label) { fontSize = Mathf.Max(18, Screen.height / 30), fontStyle = FontStyle.Bold, alignment = TextAnchor.LowerRight };
        string t = Text();
        Color c = lastFps >= 60f ? new Color(0.5f, 1f, 0.5f) : lastFps >= 30f ? new Color(1f, 0.9f, 0.3f) : new Color(1f, 0.4f, 0.3f);
        var r = new Rect(Screen.width - 520f, Screen.height - (mode == 0 ? 60f : 220f), 500f, mode == 0 ? 50f : 200f);
        st.normal.textColor = Color.black; GUI.Label(new Rect(r.x + 2, r.y + 2, r.width, r.height), t, st);
        st.normal.textColor = c; GUI.Label(r, t, st);
    }
}

// Optimizaciones aplicables en caliente (para comparar ANTES / DESPUES con el mismo recorrido).
public static class QuestPerf
{
    static bool saved;
    static int vSync, aniso, targetFps;
    static float lodBias, eyeScale = 1f;
    static bool softParticles, reflProbes;
    static readonly Dictionary<Light, LightShadows> lightShadows = new Dictionary<Light, LightShadows>();

    public static void Apply(bool on)
    {
        if (!saved)
        {
            saved = true;
            vSync = QualitySettings.vSyncCount; aniso = (int)QualitySettings.anisotropicFiltering; lodBias = QualitySettings.lodBias;
            softParticles = QualitySettings.softParticles; reflProbes = QualitySettings.realtimeReflectionProbes; targetFps = Application.targetFrameRate;
            eyeScale = XRSettings.isDeviceActive ? XRSettings.eyeTextureResolutionScale : 1f;
        }
        if (on)
        {
            QualitySettings.vSyncCount = 0;
            QualitySettings.anisotropicFiltering = AnisotropicFiltering.Disable;
            QualitySettings.lodBias = 0.7f;
            QualitySettings.softParticles = false;
            QualitySettings.realtimeReflectionProbes = false;
            Application.targetFrameRate = 72;                         // refresco nativo del Quest 2/3 (72 Hz)
            if (XRSettings.isDeviceActive) XRSettings.eyeTextureResolutionScale = 0.85f;
            Lights();
        }
        else
        {
            QualitySettings.vSyncCount = vSync;
            QualitySettings.anisotropicFiltering = (AnisotropicFiltering)aniso;
            QualitySettings.lodBias = lodBias;
            QualitySettings.softParticles = softParticles;
            QualitySettings.realtimeReflectionProbes = reflProbes;
            Application.targetFrameRate = targetFps;
            if (XRSettings.isDeviceActive) XRSettings.eyeTextureResolutionScale = eyeScale;
            foreach (var kv in lightShadows) if (kv.Key != null) kv.Key.shadows = kv.Value;
            lightShadows.Clear();
        }
    }

    // sin sombras y con alcance acotado (tambien para luces creadas despues, p. ej. antorchas de cada piso)
    public static void Lights()
    {
        foreach (var l in Object.FindObjectsByType<Light>(FindObjectsSortMode.None))
        {
            if (l.shadows != LightShadows.None) { lightShadows[l] = l.shadows; l.shadows = LightShadows.None; }
            if (l.type == LightType.Point && l.range > 14f) l.range = 14f;   // menor alcance = menos pixeles iluminados
        }
    }

    // Une en pocas mallas lo que no se mueve (menos draw calls). Solo se llama sobre raices 100 % estaticas.
    public static void Combine(GameObject root)
    {
        if (root == null || PlayerPrefs.GetInt("optimizar", 1) != 1) return;
        try { StaticBatchingUtility.Combine(root); }
        catch (System.Exception ex) { Debug.LogWarning("Combine: " + ex.Message); }
    }
}
