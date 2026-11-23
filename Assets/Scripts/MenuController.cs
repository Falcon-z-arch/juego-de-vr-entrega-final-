using UnityEngine;
using UnityEngine.SceneManagement;

// Botones del menu principal.
public class MenuController : MonoBehaviour
{
    public void StartStory()
    {
        IntroCinematic.Reset();        // que la cinematica se vea
        Load("Comarca");
    }

    // Modo VR: si/no. Se guarda y el juego lo lee al iniciar (VRRig). Con casco Quest conectado tambien se activa solo.
    public TMPro.TMP_Text vrLabel;

    void Start() { RefreshVr(); Decorate(); }

    // ---- decoracion del menu (se arma al ejecutar, no hace falta reconstruir) ----
    Transform[] stars; float[] baseScale, phase;
    Transform cam; Vector3 camPos; Transform title;
    float clock;

    static Material Emis(Color c, float k)
    {
        var tmp = GameObject.CreatePrimitive(PrimitiveType.Cube);
        var m = new Material(tmp.GetComponent<Renderer>().sharedMaterial) { color = c };
        Destroy(tmp);
        m.EnableKeyword("_EMISSION");
        if (m.HasProperty("_EmissionColor")) m.SetColor("_EmissionColor", c * k);
        return m;
    }

    void Decorate()
    {
        var c = Camera.main;
        if (c != null)
        {
            c.clearFlags = CameraClearFlags.SolidColor;
            c.backgroundColor = new Color(0.02f, 0.01f, 0.05f);
            cam = c.transform; camPos = cam.position;
        }
        RenderSettings.ambientLight = new Color(0.18f, 0.14f, 0.25f);

        // el Ascua: reloj de arena cosmico
        var asc = GameObject.Find("Ascua");
        if (asc != null && asc.GetComponent<ArtifactVisual>() == null)
        {
            asc.transform.position = new Vector3(4.6f, 0.1f, 10f);
            asc.transform.localScale = Vector3.one * 2.2f;
            asc.AddComponent<ArtifactVisual>();
        }
        var lg = GameObject.Find("LuzAscua");
        if (lg != null) { var l = lg.GetComponent<Light>(); if (l != null) { l.color = new Color(0.65f, 0.45f, 1f); l.intensity = 30f; } }

        // estrellas
        var holder = new GameObject("Estrellas").transform;
        var white = Emis(new Color(1f, 0.97f, 0.85f), 3f);
        var blue = Emis(new Color(0.6f, 0.75f, 1f), 3f);
        var rnd = new System.Random(11);
        int n = 240;
        stars = new Transform[n]; baseScale = new float[n]; phase = new float[n];
        for (int i = 0; i < n; i++)
        {
            Vector3 d = new Vector3((float)rnd.NextDouble() * 2f - 1f, (float)rnd.NextDouble() * 1.4f - 0.2f, (float)rnd.NextDouble() * 0.9f + 0.1f).normalized;
            var s = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            Destroy(s.GetComponent<Collider>());
            s.transform.SetParent(holder, false);
            s.transform.position = d * Mathf.Lerp(40f, 80f, (float)rnd.NextDouble());
            float sc = Mathf.Lerp(0.15f, 0.5f, (float)rnd.NextDouble());
            s.transform.localScale = Vector3.one * sc;
            var r = s.GetComponent<Renderer>();
            r.sharedMaterial = rnd.NextDouble() < 0.7 ? white : blue;
            r.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            stars[i] = s.transform; baseScale[i] = sc; phase[i] = (float)rnd.NextDouble() * 6.28f;
        }

        // volcan de Karnak al fondo, a la izquierda, con la lava brillando
        var rock = Emis(new Color(0.12f, 0.07f, 0.07f), 0.05f);
        var lava = Emis(new Color(1f, 0.35f, 0.05f), 2.5f);
        Vector3 vb = new Vector3(-16f, -9f, 55f);
        float[] rad = { 34f, 25f, 17f, 10f, 6f }; float[] hh = { 8f, 8f, 8f, 7f, 6f };
        float y = 0f;
        for (int i = 0; i < rad.Length; i++)
        {
            var g = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            Destroy(g.GetComponent<Collider>());
            g.transform.SetParent(holder, false);
            g.transform.position = vb + new Vector3(0f, y + hh[i] * 0.5f, 0f);
            g.transform.localScale = new Vector3(rad[i] * 2f, hh[i] * 0.5f, rad[i] * 2f);
            g.GetComponent<Renderer>().sharedMaterial = rock;
            y += hh[i];
        }
        var top = GameObject.CreatePrimitive(PrimitiveType.Sphere);
        Destroy(top.GetComponent<Collider>());
        top.transform.SetParent(holder, false);
        top.transform.position = vb + new Vector3(0f, y, 0f);
        top.transform.localScale = new Vector3(10f, 2f, 10f);
        top.GetComponent<Renderer>().sharedMaterial = lava;
        var vl = new GameObject("LuzVolcan"); vl.transform.SetParent(holder, false);
        vl.transform.position = vb + new Vector3(0f, y + 6f, -10f);
        var vlight = vl.AddComponent<Light>(); vlight.type = LightType.Point; vlight.range = 80f; vlight.intensity = 60f; vlight.color = new Color(1f, 0.4f, 0.1f);

        StyleUi();
    }

    void StyleUi()
    {
        var gold = new Color(1f, 0.78f, 0.3f);
        foreach (var b in FindObjectsByType<UnityEngine.UI.Button>(FindObjectsSortMode.None))
        {
            var im = b.GetComponent<UnityEngine.UI.Image>();
            if (im != null) im.color = new Color(0.12f, 0.07f, 0.1f, 0.9f);
            var o = b.gameObject.GetComponent<UnityEngine.UI.Outline>();
            if (o == null) o = b.gameObject.AddComponent<UnityEngine.UI.Outline>();
            o.effectColor = new Color(gold.r, gold.g, gold.b, 0.85f);
            o.effectDistance = new Vector2(3f, -3f);
            var cb = b.colors;
            cb.highlightedColor = new Color(1.6f, 1.3f, 1.0f); cb.pressedColor = new Color(0.7f, 0.6f, 0.5f);
            b.colors = cb;
            var t = b.GetComponentInChildren<TMPro.TMP_Text>();
            if (t != null) { t.color = new Color(1f, 0.93f, 0.8f); t.fontStyle = TMPro.FontStyles.Bold; }
        }
        foreach (var t in FindObjectsByType<TMPro.TextMeshProUGUI>(FindObjectsSortMode.None))
        {
            if (t.name == "Titulo")
            {
                t.text = "ASCUA";
                t.enableVertexGradient = true;
                t.colorGradient = new TMPro.VertexGradient(new Color(1f, 0.95f, 0.7f), new Color(1f, 0.9f, 0.55f), new Color(1f, 0.5f, 0.15f), new Color(0.9f, 0.35f, 0.1f));
                t.outlineWidth = 0.25f; t.outlineColor = new Color32(40, 10, 0, 255);
                title = t.transform;
            }
            else if (t.name == "Subtitulo")
            {
                t.text = "El reloj de arena de Karnak";
                t.color = new Color(0.8f, 0.7f, 1f);
                t.fontStyle = TMPro.FontStyles.Italic;
            }
        }
    }

    void Update()
    {
        clock += Time.unscaledDeltaTime;
        if (stars != null)
            for (int i = 0; i < stars.Length; i++)
                if (stars[i] != null) stars[i].localScale = Vector3.one * baseScale[i] * (1f + 0.35f * Mathf.Sin(clock * (1.5f + (i % 5) * 0.4f) + phase[i]));
        if (cam != null)
        {
            cam.position = camPos + new Vector3(Mathf.Sin(clock * 0.15f) * 0.4f, Mathf.Sin(clock * 0.11f) * 0.25f, 0f);
            cam.rotation = Quaternion.Euler(Mathf.Sin(clock * 0.2f) * 1.2f, Mathf.Sin(clock * 0.13f) * 2.5f, 0f);
        }
        if (title != null) title.localScale = Vector3.one * (1f + 0.025f * Mathf.Sin(clock * 1.4f));
    }

    public void ToggleVR()
    {
        PlayerPrefs.SetInt("modoVR", PlayerPrefs.GetInt("modoVR", 0) == 1 ? 0 : 1);
        PlayerPrefs.Save();
        RefreshVr();
    }

    void RefreshVr()
    {
        if (vrLabel != null) vrLabel.text = "Modo VR: " + (PlayerPrefs.GetInt("modoVR", 0) == 1 ? "SI" : "NO");
    }

    public void StartTest() => Load("ZonaPruebas");

    public void StartGallery() => Load("Galeria");

    public void Quit()
    {
        Application.Quit();
#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
#endif
    }

    void Load(string scene)
    {
        Time.timeScale = 1f;
        if (Application.CanStreamedLevelBeLoaded(scene)) SceneManager.LoadScene(scene);
        else Debug.LogWarning("Falta la escena " + scene + ". Ejecuta Certamen2 > 0 - Construir TODO.");
    }
}
