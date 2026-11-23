using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

// Cinematica de inicio del modo historia: escenario 3D cosmico (estrellas, el reloj de arena, el Cronarca, los viajeros),
// camara que se mueve en cada escena, barras de cine y texto con maquina de escribir.
// Toca la pantalla (o espacio) para saltarla. Solo se muestra una vez por partida.
public class IntroCinematic : MonoBehaviour
{
    public static bool Pending;     // true mientras falta mostrarla
    static bool shown;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    static void ResetStatics() { Pending = false; shown = false; CosmosTime.Ignore = false; }

    // el menu lo llama para que la cinematica vuelva a verse
    public static void Reset() { shown = false; }

    // texto, escena 3D
    static readonly string[] Cards =
    {
        "ASCUA\n<size=45%>El reloj de arena de Karnak</size>",
        "Hace mil años, el Heraldo forjo el Ascua: un reloj de arena hecho de estrellas, fragmento del Reloj del Mundo.\nQuien la porta puede doblar el tiempo... y el tiempo le cobra con cada paso.",
        "El Heraldo se convirtio en el Cronarca, guardian del Monte Karnak.\nMientras el Ascua exista, el tiempo se repite.",
        "Cada vez que un viajero cae, el tiempo retrocede y despierta de nuevo en la Comarca.\nNo recuerda el camino, pero el mundo guarda lo que gano con esfuerzo.",
        "Tres viajeros se ofrecen a llevarla.\nUn hobbit de Cotollano, pequeño y resistente.\nUna maga que rompe la llama con luz.\nUn ingeniero con un gancho y mil trucos.\n<size=70%>Y un peleador, para quien se atreva en realidad virtual.</size>",
        "Cuatro pisos de mazmorra, y en la cima, el Cronarca.\n\nSube. Relevate. Resiste.\nY esta vez, que el tiempo siga adelante."
    };
    static readonly int[] Shots = { 0, 1, 3, 1, 2, 4 };   // 0 acercamiento, 1 orbita, 3 Cronarca, 2 viajeros, 4 final

    // ---- escenario 3D ----
    static readonly Vector3 O = new Vector3(0f, -3000f, 0f);
    Transform stage, stars;
    Camera cam;
    GameObject hourglass, cronarca;
    Transform[] orbs;
    Light la, lb;
    int shot;
    float shotT, clock;
    Material starMat, starMat2;

    void Awake()
    {
        Pending = !shown;
    }

    void Start()
    {
        if (shown) { Pending = false; Destroy(this); return; }
        StartCoroutine(Run());
    }

    Material Emis(Color c, float k)
    {
        var tmp = GameObject.CreatePrimitive(PrimitiveType.Cube);
        var m = new Material(tmp.GetComponent<Renderer>().sharedMaterial) { color = c };
        Destroy(tmp);
        m.EnableKeyword("_EMISSION");
        if (m.HasProperty("_EmissionColor")) m.SetColor("_EmissionColor", c * k);
        return m;
    }

    void BuildStage()
    {
        stage = new GameObject("EscenarioIntro").transform;
        stage.position = O;

        var cgo = new GameObject("CamaraIntro", typeof(Camera));
        cam = cgo.GetComponent<Camera>();
        cam.clearFlags = CameraClearFlags.SolidColor;
        cam.backgroundColor = new Color(0.01f, 0.005f, 0.03f);
        cam.depth = 100f; cam.fieldOfView = 48f; cam.nearClipPlane = 0.1f; cam.farClipPlane = 400f;
        cgo.transform.SetParent(stage, false);

        // estrellas
        stars = new GameObject("Estrellas").transform;
        stars.SetParent(stage, false);
        stars.localPosition = new Vector3(0f, 0f, 12f);
        starMat = Emis(new Color(1f, 0.97f, 0.85f), 3f);
        starMat2 = Emis(new Color(0.6f, 0.75f, 1f), 3f);
        var rnd = new System.Random(5);
        for (int i = 0; i < 280; i++)
        {
            Vector3 d = new Vector3((float)rnd.NextDouble() * 2f - 1f, (float)rnd.NextDouble() * 2f - 1f, (float)rnd.NextDouble() * 2f - 1f);
            if (d.sqrMagnitude < 0.01f) continue;
            d.Normalize();
            var s = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            Destroy(s.GetComponent<Collider>());
            s.transform.SetParent(stars, false);
            s.transform.localPosition = d * Mathf.Lerp(30f, 70f, (float)rnd.NextDouble());
            s.transform.localScale = Vector3.one * Mathf.Lerp(0.1f, 0.35f, (float)rnd.NextDouble());
            var r = s.GetComponent<Renderer>();
            r.sharedMaterial = rnd.NextDouble() < 0.7 ? starMat : starMat2;
            r.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
        }

        // el reloj de arena (Ascua)
        var hg = GameObject.CreatePrimitive(PrimitiveType.Sphere);
        hg.name = "AscuaIntro";
        Destroy(hg.GetComponent<Collider>());
        hg.transform.SetParent(stage, false);
        hg.transform.localPosition = new Vector3(0f, 0f, 12f);
        hg.transform.localScale = Vector3.one * 2.2f;
        hg.AddComponent<ArtifactVisual>();
        hourglass = hg;

        // el Cronarca
        cronarca = new GameObject("CronarcaIntro");
        cronarca.transform.SetParent(stage, false);
        cronarca.transform.localPosition = new Vector3(0f, 0f, 12f);
        cronarca.transform.localScale = Vector3.one * 0.8f;
        CronarcaLook.Build(cronarca.transform);
        cronarca.SetActive(false);

        // los viajeros (orbes de colores)
        Color[] cs = { new Color(0.4f, 0.9f, 0.3f), new Color(0.7f, 0.4f, 1f), new Color(1f, 0.6f, 0.15f), new Color(1f, 0.25f, 0.2f) };
        orbs = new Transform[4];
        for (int i = 0; i < 4; i++)
        {
            var o = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            Destroy(o.GetComponent<Collider>());
            o.name = "Viajero" + i;
            o.transform.SetParent(stage, false);
            o.transform.localScale = Vector3.one * 0.55f;
            o.GetComponent<Renderer>().sharedMaterial = Emis(cs[i], 2.5f);
            var lg = new GameObject("Luz"); lg.transform.SetParent(o.transform, false);
            var l = lg.AddComponent<Light>(); l.type = LightType.Point; l.range = 8f; l.intensity = 6f; l.color = cs[i];
            orbs[i] = o.transform;
            o.SetActive(false);
        }

        // luces de ambiente
        var a = new GameObject("LuzA"); a.transform.SetParent(stage, false); a.transform.localPosition = new Vector3(-8f, 5f, 4f);
        la = a.AddComponent<Light>(); la.type = LightType.Point; la.range = 40f; la.intensity = 30f; la.color = new Color(0.6f, 0.4f, 1f);
        var b = new GameObject("LuzB"); b.transform.SetParent(stage, false); b.transform.localPosition = new Vector3(8f, -3f, 6f);
        lb = b.AddComponent<Light>(); lb.type = LightType.Point; lb.range = 40f; lb.intensity = 24f; lb.color = new Color(0.2f, 0.8f, 1f);
    }

    // se llama cada cuadro (Update corre aunque el timeScale sea 0)
    void Update()
    {
        if (stage == null) return;
        float dt = Time.unscaledDeltaTime;
        clock += dt; shotT += dt;
        stars.Rotate(0.4f * dt, 2f * dt, 0.3f * dt, Space.Self);

        Vector3 focus = O + new Vector3(0f, 0f, 12f);
        Vector3 pos; Vector3 look = focus;
        Color ca = new Color(0.6f, 0.4f, 1f), cb = new Color(0.2f, 0.8f, 1f);

        hourglass.SetActive(shot != 3);
        cronarca.SetActive(shot == 3);

        switch (shot)
        {
            case 0:   // plano que avanza hacia el reloj
                pos = Vector3.Lerp(O + new Vector3(0f, 1.2f, -5f), O + new Vector3(0f, 0.6f, 4f), Mathf.SmoothStep(0f, 1f, shotT / 10f));
                break;
            case 1:   // orbita
            {
                float a = shotT * 0.22f + 0.5f;
                pos = focus + new Vector3(Mathf.Sin(a) * 8.5f, 1.2f + Mathf.Sin(shotT * 0.5f), -Mathf.Cos(a) * 8.5f);
                break;
            }
            case 3:   // el Cronarca desde abajo, luces rojas
                pos = focus + new Vector3(3.5f + Mathf.Sin(shotT * 0.3f) * 1.5f, -2.8f, -9f + shotT * 0.12f);
                look = focus + Vector3.up * 0.8f;
                ca = new Color(1f, 0.3f, 0.1f); cb = new Color(1f, 0.7f, 0.2f);
                break;
            case 2:   // viajeros: los orbes giran alrededor del reloj
            {
                float a = -shotT * 0.18f;
                pos = focus + new Vector3(Mathf.Sin(a) * 7f, 0.8f, -Mathf.Cos(a) * 7f);
                break;
            }
            default:  // final: los orbes se funden en el reloj y la camara se aleja
                pos = focus + new Vector3(0f, 1.5f + shotT * 0.1f, -6f - shotT * 0.9f);
                break;
        }
        cam.transform.position = pos;
        cam.transform.rotation = Quaternion.LookRotation((look - pos).normalized, Vector3.up);

        la.color = Color.Lerp(la.color, ca, dt * 2f);
        lb.color = Color.Lerp(lb.color, cb, dt * 2f);

        // orbes de los viajeros (escenas 2 y 4 = indices de Shots 3/4 segun el caso)
        bool orbsOn = shot == 2 || shot == 4;
        for (int i = 0; i < orbs.Length; i++)
        {
            if (orbs[i].gameObject.activeSelf != orbsOn) orbs[i].gameObject.SetActive(orbsOn);
            if (!orbsOn) continue;
            float k = shot == 4 ? Mathf.Clamp01(1f - shotT / 7f) : 1f;      // en el final se acercan al centro
            float r = Mathf.Lerp(0.3f, 4f, k) + 0.4f * Mathf.Sin(clock * 2f + i);
            float ang = clock * (0.9f + 0.15f * i) + i * Mathf.PI * 0.5f;
            orbs[i].position = focus + new Vector3(Mathf.Sin(ang) * r, Mathf.Sin(ang * 1.7f + i) * 1.2f * k, Mathf.Cos(ang) * r);
        }
    }

    IEnumerator Run()
    {
        shown = true;
        float oldScale = Time.timeScale;
        Time.timeScale = 0f;
        CosmosTime.Ignore = true;
        MusicPlayer.Play("intro");
        BuildStage();

        // capa de pantalla completa
        var canvasGO = new GameObject("CinematicaCanvas", typeof(Canvas), typeof(CanvasScaler));
        var canvas = canvasGO.GetComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 500;
        var scaler = canvasGO.GetComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920, 1080);
        scaler.matchWidthOrHeight = 0.5f;

        // fundido negro inicial (el escenario se revela)
        var bg = NewRect("Fondo", canvasGO.transform);
        Stretch(bg);
        var bgImg = bg.gameObject.AddComponent<Image>();
        bgImg.color = Color.black;
        bgImg.raycastTarget = false;

        // barras de cine
        var top = Bar(canvasGO.transform, true);
        var bot = Bar(canvasGO.transform, false);

        // franja oscura detras del texto
        var band = NewRect("FranjaTexto", canvasGO.transform);
        band.anchorMin = new Vector2(0f, 0.16f); band.anchorMax = new Vector2(1f, 0.46f);
        band.offsetMin = band.offsetMax = Vector2.zero;
        var bandImg = band.gameObject.AddComponent<Image>();
        bandImg.color = new Color(0f, 0f, 0.03f, 0f);
        bandImg.raycastTarget = false;

        var textRT = NewRect("Texto", canvasGO.transform);
        textRT.anchorMin = new Vector2(0.1f, 0.16f); textRT.anchorMax = new Vector2(0.9f, 0.46f);
        textRT.offsetMin = textRT.offsetMax = Vector2.zero;
        var text = textRT.gameObject.AddComponent<TextMeshProUGUI>();
        text.alignment = TextAlignmentOptions.Center;
        text.color = new Color(1f, 0.93f, 0.8f, 0f);
        text.fontSize = 50;
        text.outlineWidth = 0.18f;
        text.outlineColor = new Color32(0, 0, 0, 255);
        text.raycastTarget = false;

        var hintRT = NewRect("Aviso", canvasGO.transform);
        hintRT.anchorMin = hintRT.anchorMax = new Vector2(1f, 0f);
        hintRT.pivot = new Vector2(1f, 0f);
        hintRT.anchoredPosition = new Vector2(-40f, 14f);
        hintRT.sizeDelta = new Vector2(700f, 50f);
        var hint = hintRT.gameObject.AddComponent<TextMeshProUGUI>();
        hint.text = "Toca para saltar";
        hint.fontSize = 30;
        hint.alignment = TextAlignmentOptions.BottomRight;
        hint.color = new Color(1f, 1f, 1f, 0.5f);
        hint.raycastTarget = false;

        // revelar el escenario
        float f0 = 1f;
        while (f0 > 0f)
        {
            f0 -= Time.unscaledDeltaTime / 1.4f;
            bgImg.color = new Color(0f, 0f, 0f, Mathf.Clamp01(f0));
            SetBars(top, bot, 1f - Mathf.Clamp01(f0));
            yield return null;
        }

        bool skip = false;
        for (int c = 0; c < Cards.Length && !skip; c++)
        {
            string full = Cards[c];
            bool title = c == 0;
            if (shot != Shots[c]) { shot = Shots[c]; shotT = 0f; }
            text.fontSize = title ? 150 : 50;
            text.text = full;
            text.maxVisibleCharacters = 0;

            // aparecer + maquina de escribir
            float t = 0f, te = 0f;
            text.ForceMeshUpdate();
            int count = Mathf.Max(1, text.textInfo.characterCount);
            float typeTime = title ? 0.01f : Mathf.Max(1.5f, full.Length * 0.03f);
            while ((t < 1f || te < typeTime) && !skip)
            {
                t += Time.unscaledDeltaTime / 0.8f;
                te += Time.unscaledDeltaTime;
                var col = text.color; col.a = Mathf.Clamp01(t); text.color = col;
                bandImg.color = new Color(0f, 0f, 0.03f, 0.55f * Mathf.Clamp01(t) * (title ? 0f : 1f));
                text.maxVisibleCharacters = title ? 9999 : Mathf.CeilToInt(count * Mathf.Clamp01(te / typeTime));
                if (Pressed()) skip = true;
                yield return null;
            }
            text.maxVisibleCharacters = 9999;

            // leer
            float hold = title ? 4f : Mathf.Max(4f, full.Length * 0.05f);
            float h = 0f;
            while (h < hold && !skip)
            {
                h += Time.unscaledDeltaTime;
                if (Pressed()) skip = true;
                yield return null;
            }

            // desvanecer
            t = 1f;
            while (t > 0f)
            {
                t -= Time.unscaledDeltaTime / 0.7f;
                var col = text.color; col.a = Mathf.Clamp01(t); text.color = col;
                bandImg.color = new Color(0f, 0f, 0.03f, 0.55f * Mathf.Clamp01(t));
                if (skip) t -= Time.unscaledDeltaTime * 4f;
                yield return null;
            }
        }

        // destello + fundido final hacia el juego
        float f = 0f;
        while (f < 1f)
        {
            f += Time.unscaledDeltaTime / 1.2f;
            bgImg.color = new Color(1f, 0.95f, 0.8f, Mathf.Clamp01(f) * 0.9f);
            hint.color = new Color(1f, 1f, 1f, 0.5f * (1f - Mathf.Clamp01(f)));
            yield return null;
        }
        Destroy(stage.gameObject);
        Destroy(cam.gameObject);
        stage = null;
        CosmosTime.Ignore = false;
        Time.timeScale = oldScale <= 0f ? 1f : oldScale;
        f = 1f;
        while (f > 0f)
        {
            f -= Time.unscaledDeltaTime / 1.0f;
            bgImg.color = new Color(1f, 0.95f, 0.8f, Mathf.Clamp01(f) * 0.9f);
            SetBars(top, bot, Mathf.Clamp01(f));
            yield return null;
        }

        Destroy(canvasGO);
        Pending = false;
        MusicPlayer.Play("volcan");
        HUD.Say("Sube el volcan. Cuidado con las rocas");
        Destroy(this);
    }

    void OnDestroy()
    {
        CosmosTime.Ignore = false;
        if (stage != null) Destroy(stage.gameObject);
    }

    RectTransform Bar(Transform parent, bool top)
    {
        var r = NewRect(top ? "BarraArriba" : "BarraAbajo", parent);
        r.anchorMin = new Vector2(0f, top ? 1f : 0f); r.anchorMax = new Vector2(1f, top ? 1f : 0f);
        r.pivot = new Vector2(0.5f, top ? 1f : 0f);
        r.sizeDelta = new Vector2(0f, 0f);
        var im = r.gameObject.AddComponent<Image>();
        im.color = Color.black; im.raycastTarget = false;
        return r;
    }

    static void SetBars(RectTransform top, RectTransform bot, float k)
    {
        float h = 110f * k;
        top.sizeDelta = new Vector2(0f, h);
        bot.sizeDelta = new Vector2(0f, h);
    }

    static bool Pressed()
    {
        var p = Pointer.current;
        if (p != null && p.press.wasPressedThisFrame) return true;
        var k = Keyboard.current;
        return k != null && (k.spaceKey.wasPressedThisFrame || k.escapeKey.wasPressedThisFrame || k.enterKey.wasPressedThisFrame);
    }

    static RectTransform NewRect(string name, Transform parent)
    {
        var go = new GameObject(name, typeof(RectTransform));
        go.transform.SetParent(parent, false);
        return (RectTransform)go.transform;
    }

    static void Stretch(RectTransform r)
    {
        r.anchorMin = Vector2.zero; r.anchorMax = Vector2.one;
        r.offsetMin = r.offsetMax = Vector2.zero;
    }
}
