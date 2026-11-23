using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

// Director de la partida (roguelike) en una montana continua: aldea al pie, puerta de Karnak,
// 4 zonas con un puzzle al azar cada una, y la cima con el jefe.
// Al abrir la puerta empieza el bombardeo.
public class RunManager : MonoBehaviour
{
    public static RunManager I;

    public PartyManager party;
    public FallingRockSpawner spawner;
    public AvalancheDirector director;
    public SummitArena summit;
    public CameraFollow cam;
    public MountainZone[] zones;          // las 4 terrazas (las asigna el editor)
    public float bombardFromX = 55f;      // el bombardeo solo cae pasada la puerta

    public bool BossDead { get; private set; }
    public bool Started { get; private set; }

    bool failed;
    float baseCalm = 0.4f, baseBurst = 2.2f;

    const int A = 0, B = 1, C = 2, D = 3, E = 4;   // tipos de puzzle

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    static void ResetStatics() => I = null;

    void Awake()
    {
        I = this;
        RunState.InRun = false;   // la aldea siempre empieza fuera de partida
        Time.timeScale = 1f;
    }

    void Start()
    {
        if (DevMode.PendingJump >= 0) StartCoroutine(DevJumpNextFrame());
        // si te caes en la aldea, reapareces en el inicio
        if (party != null && party.Current != null) Checkpoint.SetPoint(party.Current.transform.position + Vector3.up * 0.3f);
        if (director != null) { baseCalm = director.calmIntensity; baseBurst = director.burstIntensity; director.enabled = false; }
        if (spawner != null) { spawner.minAltitude = -100f; spawner.height = 26f; spawner.enabled = false; }
    }

    // ------------------------------------------------------------------ modo desarrollador
    IEnumerator DevJumpNextFrame()
    {
        yield return null;
        yield return null;
        int n = DevMode.PendingJump;
        DevMode.PendingJump = -1;
        DevJump(n);
    }

    // 0 = aldea, 1..4 = piso, 5 = jefe final
    public void DevJump(int n)
    {
        if (party == null || party.Current == null) return;
        Vector3 pos;
        if (n <= 0)
            pos = new Vector3(-48f, 0.3f, 0f);
        else
        {
            if (!Started) StartClimb();
            if (n >= 5 && summit != null && summit.startPoint != null) pos = summit.startPoint.position + Vector3.up * 1.5f;
            else
            {
                var z = zones[Mathf.Clamp(n, 1, zones.Length) - 1];
                pos = z.transform.position + new Vector3(-14f, 1.4f, 0f);
            }
        }
        Checkpoint.SetPoint(pos);
        party.Current.Teleport(pos);
        if (n >= 5)
        {
            DevBossKit(pos);
            var arena = summit != null ? summit : FindFirstObjectByType<SummitArena>(FindObjectsInactive.Include);
            if (arena != null) arena.Begin();
            else
            {
                var bc = FindFirstObjectByType<BossController>(FindObjectsInactive.Include);
                if (bc != null) { bc.gameObject.SetActive(true); OnSummit(); }
                else HUD.Say("DEV: no encuentro al jefe en la escena (reconstruir Certamen2 > 0)");
            }
        }
        HUD.Say(n <= 0 ? "DEV: aldea" : n >= 5 ? "DEV: jefe final" : "DEV: piso " + n);
    }

    // Para probar al jefe: deja 4 cubos (4 de dano al lanzarlos) y armas del Peleador junto al inicio de la cima
    void DevBossKit(Vector3 pos)
    {
        Vector3 fwd = Vector3.right;
        for (int i = 0; i < 4; i++)
        {
            var c = GameObject.CreatePrimitive(PrimitiveType.Cube);
            c.name = "Cubo_DEV";
            c.transform.localScale = Vector3.one * 0.8f;
            c.GetComponent<Renderer>().material.color = new Color(0.3f, 0.5f, 1f);
            c.transform.position = pos + fwd * 2.5f + Vector3.up * (0.6f + i * 0.9f) + Vector3.forward * (i % 2 == 0 ? 1.2f : -1.2f);
            c.AddComponent<PuzzleCube>();
        }
        WeaponItem.Kind[] ks = { WeaponItem.Kind.Sword, WeaponItem.Kind.Axe, WeaponItem.Kind.Mace, WeaponItem.Kind.Shield };
        for (int i = 0; i < ks.Length; i++)
            WeaponItem.Make(ks[i], pos + fwd * 1.5f + Vector3.forward * (-3f + i * 1.8f) + Vector3.up * 0.6f);
    }

    // Borra las puertas de ese piso (y copias) y crea una nueva, limpia; despues despeja el camino hasta el piso siguiente
    void RebuildDoor(MountainZone z)
    {
        var bc = z.GetComponent<BoxCollider>();
        float half = bc != null ? (bc.size.x + 3f) * 0.5f : 30f;
        float xe = z.transform.position.x + half, h1 = z.transform.position.y;
        Vector3 cp = new Vector3(xe + 2f, h1 + 2.2f, 0f);
        int removed = 0;
        foreach (var d in FindObjectsByType<MountainDoor>(FindObjectsSortMode.None))
            if (Mathf.Abs(d.transform.position.x - xe) < 5f && Mathf.Abs(d.transform.position.y - (h1 + 3f)) < 8f)
            {
                if (d.checkpointAfter != Vector3.zero) cp = d.checkpointAfter;
                d.gameObject.name = "PuertaVieja_borrada";
                d.gameObject.SetActive(false);
                Destroy(d.gameObject);
                removed++;
            }
        var g = GameObject.CreatePrimitive(PrimitiveType.Cube);
        g.name = "Puerta_Zona" + z.index + "_nueva";
        g.transform.position = new Vector3(xe, h1 + 3f, 0f);
        g.transform.localScale = new Vector3(1.2f, 6f, 12f);
        var rb = g.AddComponent<Rigidbody>(); rb.isKinematic = true; rb.useGravity = false;
        var md = g.AddComponent<MountainDoor>();
        md.rend = g.GetComponent<Renderer>();
        md.checkpointAfter = cp;
        z.door = md;
        int cleared = ClearPath(xe, h1, md);
        Debug.Log("[RunManager] piso " + z.index + ": puertas borradas " + removed + ", obstaculos quitados " + cleared + ", puerta nueva en x=" + xe + " y=" + (h1 + 3f));
    }

    // quita lo que estorbe en el pasillo (|z|<5.5) desde el fondo de la terraza hasta arriba de la rampa siguiente
    int ClearPath(float xe, float h1, MountainDoor keep)
    {
        int n = 0;
        var seen = new HashSet<GameObject>();
        for (float x = xe - 4f; x <= xe + 27f; x += 1.5f)
        {
            float sy = x <= xe ? h1 : h1 + (x - xe) / 26f * 12f;
            var hits = Physics.OverlapBox(new Vector3(x, sy + 0.9f + 1.5f, 0f), new Vector3(1.0f, 1.5f, 5.5f), Quaternion.identity, ~0, QueryTriggerInteraction.Ignore);
            foreach (var c in hits)
            {
                if (c == null) continue;
                var go = c.gameObject;
                if (keep != null && go == keep.gameObject) continue;
                string nm = go.name;
                if (nm.StartsWith("Rampa") || nm.StartsWith("Terraza") || nm.StartsWith("Baranda") || nm.StartsWith("Muro") || nm.StartsWith("Cima") || nm.StartsWith("Base") || nm.StartsWith("Monte")) continue;
                if (go.GetComponentInParent<PlayerMovement>() != null) continue;
                if (go.GetComponentInParent<Enemy>() != null) continue;
                if (go.GetComponentInParent<MountainDoor>() != null) continue;
                if (go.GetComponentInParent<Coin>() != null) continue;
                if (nm.Contains("heckpoint") || nm.Contains("Beacon") || nm.Contains("Baliza")) continue;
                if (!seen.Add(go)) continue;
                Debug.Log("[RunManager] quito obstaculo del pasillo: " + nm + " en " + c.bounds.center);
                c.enabled = false;
                var r = go.GetComponent<Renderer>(); if (r != null && !nm.Contains("Torre")) r.enabled = false;
                n++;
            }
        }
        return n;
    }

    // la puerta REAL de una zona, por geometria (no por la referencia guardada en la escena, que puede apuntar a otra copia)
    public static MountainDoor DoorOf(MountainZone z)
    {
        if (z == null) return null;
        var bc = z.GetComponent<BoxCollider>();
        float half = bc != null ? (bc.size.x + 3f) * 0.5f : 0f;
        Vector2 target = new Vector2(z.transform.position.x + half, z.transform.position.y + 3f);
        MountainDoor best = null; float bd = 1e9f;
        foreach (var d in FindObjectsByType<MountainDoor>(FindObjectsSortMode.None))
        {
            Vector2 p = new Vector2(d.transform.position.x, d.transform.position.y);
            float dd = (p - target).sqrMagnitude;
            if (dd < bd) { bd = dd; best = d; }
        }
        if (best != null && bc != null && bd < 25f) return best;   // a menos de 5 m del sitio esperado
        return z.door != null ? z.door : best;
    }

    // abre TODAS las puertas que esten en el lugar de la puerta de ese piso (por si hay copias superpuestas)
    void OpenDoorsOfFloor(int floor)
    {
        foreach (var z in FindObjectsByType<MountainZone>(FindObjectsSortMode.None))
        {
            if (z.index != floor) continue;
            var main = DoorOf(z);
            if (main == null) continue;
            foreach (var d in FindObjectsByType<MountainDoor>(FindObjectsSortMode.None))
                if (d == main || (Mathf.Abs(d.transform.position.x - main.transform.position.x) < 3f && Mathf.Abs(d.transform.position.y - main.transform.position.y) < 4f))
                    d.Activate();
        }
    }

    // DEV: abre las puertas de los 4 pisos sin resolver los puzzles
    public void DevOpenDoors()
    {
        if (!Started) StartClimb();
        for (int i = 1; i <= 4; i++) OpenDoorsOfFloor(i);
        HUD.Say("DEV: puertas abiertas");
    }

    // DEV: abre la puerta de un piso concreto (1..4)
    public void DevOpenDoor(int floor)
    {
        if (!Started) StartClimb();
        if (floor < 1 || floor > 4) return;
        OpenDoorsOfFloor(floor);
        HUD.Say("DEV: puerta del piso " + floor + " abierta");
    }

    // lo llama la Puerta de Karnak
    public void StartClimb()
    {
        if (Started) return;
        Started = true;
        RunState.StartRun();
        RunState.Floor = 0;

        if (zones != null)
            for (int i = 0; i < zones.Length; i++)
                try { FillZone(zones[i]); } catch (System.Exception ex) { Debug.LogError("FillZone piso " + (i + 1) + ": " + ex); HUD.Say("ERROR piso " + (i + 1) + ": " + ex.Message); }

        EnsureZones();
        if (spawner != null) { spawner.minX = bombardFromX; spawner.enabled = true; }
        SetHazards(0.9f);
        if (director != null) director.enabled = true;

        Vector3 p = party.Current.transform.position;
        Checkpoint.SetPoint(new Vector3(Mathf.Max(p.x, 64f), 0.3f, 0f));
        MusicPlayer.Play("volcan", 0.4f);
        HUD.Say("¡La puerta se abre y comienza el bombardeo! Sube la montana");
    }

    public void EnterZone(MountainZone z)
    {
        if (!Started || z == null) return;
        if (RunState.Floor == z.index) return;
        if (z.content == null) { try { FillZone(z); } catch (System.Exception ex) { Debug.LogError("FillZone(lazy) " + ex); } }
        RunState.Floor = z.index;
        SetHazards(0.8f + 0.2f * z.index);
        EnemySpawner.Populate(z);
        HUD.Say("Piso " + z.index + ": " + z.title + ". Resuelve el puzzle para abrir la puerta");
        if (z.index == 1)
            Tutorial.Show("puzzle_equipo", "Puzzles en equipo",
                "Cada piso se resuelve con DOS personajes:\n\n- El HOBBIT: se agacha (boton Habilidad) y cabe por la gatera baja.\n- El MAGO o el INGENIERO, a eleccion: el mago dispara a los cristales; el ingeniero usa el gancho.\n\nCambia de personaje con el boton Personajes. El avance no se pierde al cambiar.");
    }

    public void OnSummit()
    {
        RunState.Floor = 5;
        SetHazards(1.3f);
        MusicPlayer.Play("boss", 0.4f);
        HUD.Say("El Cronarca te espera. Magia o cubos para herirlo");
    }

    // (lo usa FloorExit de la galeria; en la montana ya no hay portales)
    public void NextFloor() { }

    void SetHazards(float mul)
    {
        if (director == null) return;
        director.calmIntensity = baseCalm * mul;
        director.burstIntensity = baseBurst * mul;
    }

    // ------------------------------------------------------------------ llenado de zonas
    // por si el array 'zones' del editor no trae algun piso: llena toda zona de la escena que siga vacia
    void EnsureZones()
    {
        int filled = 0, total = 0;
        foreach (var z in FindObjectsByType<MountainZone>(FindObjectsSortMode.None))
        {
            total++;
            if (z.content == null)
            {
                try { FillZone(z); } catch (System.Exception ex) { Debug.LogError("EnsureZones piso " + z.index + ": " + ex); HUD.Say("ERROR piso " + z.index + ": " + ex.Message); }
            }
            if (z.content != null) filled++;
        }
        Debug.Log("[RunManager] zonas llenas " + filled + "/" + total + " (array zones: " + (zones == null ? 0 : zones.Length) + ")");
        // puertas sin chequeo (no se les creo decoracion): se les pone el panel basico
        foreach (var d in FindObjectsByType<MountainDoor>(FindObjectsSortMode.None))
            if (d.GetComponent<DoorCheck>() == null)
                Debug.LogWarning("[RunManager] puerta sin DoorCheck: " + d.name);
    }

    void FillZone(MountainZone z)
    {
        if (z == null) return;
        int f = z.index;
        var content = new GameObject("Contenido").transform;
        content.SetParent(z.transform, false);
        z.content = content;
        try { RebuildDoor(z); } catch (System.Exception ex) { Debug.LogError("RebuildDoor piso " + f + ": " + ex); z.door = DoorOf(z); }

        var sinkGO = new GameObject("Compuerta");
        sinkGO.transform.SetParent(content, false);
        var sink = sinkGO.AddComponent<ActivationCounter>();
        sink.targets = new Activatable[] { z.door };

        try { sink.required = CoopBuilder.Build(content, sink); }
        catch (System.Exception ex) { Debug.LogError("CoopBuilder piso " + f + ": " + ex); HUD.Say("ERROR puzzle piso " + f + ": " + ex.Message); if (sink.required < 1) sink.required = 1; }
        if (z.door != null)
        {
            try { DoorDecor.Build(content, z.door, CoopBuilder.LastLatches); }
            catch (System.Exception ex)
            {
                Debug.LogError("DoorDecor: " + ex);
                var dc = z.door.gameObject.AddComponent<DoorCheck>();
                dc.door = z.door; dc.latches = CoopBuilder.LastLatches;
            }
        }

        try {
        // refugios
        FB.Shelter(content, new Vector3(-5f, 0f, -13f));
        FB.Shelter(content, new Vector3(-5f, 0f, 13f));
        FB.Shelter(content, new Vector3(15f, 0f, 12f));

        // cofre (zonas 1 y 3) o altar de reliquias (zonas 2 y 4), en la zona de inicio
        if (f % 2 == 1) FB.MakeChest(content, new Vector3(-15f, 0f, 3f), 6 + f * 2);
        else FB.MakeAltar(content, new Vector3(-15f, 0f, 3f));

        } catch (System.Exception ex) { Debug.LogError("Decor piso " + f + ": " + ex); }

        // monedas
        for (int i = 0; i < 5 + f; i++)
        {
            Vector3 lp = new Vector3(Random.Range(-15f, 15f), 0.9f, Random.Range(-13f, 13f));
            for (int tries = 0; tries < 20 && BadCoinSpot(lp); tries++)
                lp = new Vector3(Random.Range(-15f, 15f), 0.9f, Random.Range(-13f, 13f));
            Vector3 p = content.TransformPoint(lp);
            var cc = Coin.Spawn(p, 1);
            cc.transform.SetParent(content, true);
        }
    }

    // evita monedas dentro de la caseta, sobre la lava o dentro de la torre
    static bool BadCoinSpot(Vector3 p)
    {
        if (p.x > -16f && p.x < -8f && Mathf.Abs(p.z) > 6.5f) return true;
        if (p.x > -2f && p.x < 12f && Mathf.Abs(p.z) > 3f) return true;
        if (p.x > 1f && p.x < 7f && Mathf.Abs(p.z) < 3.5f) return true;
        return false;
    }

    List<int> PickPuzzles(int f)
    {
        int[] pool;
        int n;
        switch (f)
        {
            case 1: pool = new[] { A, D, E }; n = 1; break;
            case 2: pool = new[] { B, C }; n = 1; break;
            case 3: pool = new[] { A, B, C, D, E }; n = 2; break;
            default: pool = new[] { B, C, D, E }; n = 2; break;
        }
        var list = new List<int>(pool);
        var res = new List<int>();
        while (res.Count < n && list.Count > 0)
        {
            int i = Random.Range(0, list.Count);
            res.Add(list[i]);
            list.RemoveAt(i);
        }
        return res;
    }

    // ------------------------------------------------------------------ derrota y victoria
    public void Fail()
    {
        if (failed) return;
        failed = true;
        StartCoroutine(FailRoutine());
    }

    IEnumerator FailRoutine()
    {
        party.Current.locked = true;
        int gained = RunState.EndRun(false);
        var ov = Overlay.Create("EL TIEMPO RETROCEDE...\n\nMonedas guardadas: " + gained, new Color(0.35f, 0.03f, 0.03f, 1f));
        MusicPlayer.Stop();
        yield return ov.FadeIn(0.8f);
        yield return new WaitForSecondsRealtime(3f);
        Time.timeScale = 1f;
        GoHome();
    }

    public void BossDefeated()
    {
        BossDead = true;
        HUD.Say("El tiempo vuelve a fluir. Arroja el Ascua al crater");
        Sfx.Beep(880f, 0.4f);
    }

    // lo llama la cinematica final
    public int Win()
    {
        return RunState.EndRun(true);
    }

    public static void GoHome()
    {
        Time.timeScale = 1f;
        if (Application.CanStreamedLevelBeLoaded("Comarca")) SceneManager.LoadScene("Comarca");
        else if (Application.CanStreamedLevelBeLoaded("Menu")) SceneManager.LoadScene("Menu");
    }
}

// Pantalla con texto y fundidos (usa tiempo real: sirve con el juego en pausa)
public class Overlay
{
    GameObject go;
    Image img;
    TMP_Text txt;
    Color baseColor;

    public static Overlay Create(string text, Color color, int fontSize = 90)
    {
        var o = new Overlay { baseColor = color };
        o.go = new GameObject("OverlayCanvas", typeof(Canvas), typeof(CanvasScaler));
        var canvas = o.go.GetComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 450;
        var sc = o.go.GetComponent<CanvasScaler>();
        sc.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        sc.referenceResolution = new Vector2(1920, 1080);
        sc.matchWidthOrHeight = 0.5f;

        var bg = new GameObject("Fondo", typeof(RectTransform), typeof(Image));
        bg.transform.SetParent(o.go.transform, false);
        var brt = (RectTransform)bg.transform;
        brt.anchorMin = Vector2.zero; brt.anchorMax = Vector2.one; brt.offsetMin = brt.offsetMax = Vector2.zero;
        o.img = bg.GetComponent<Image>();
        o.img.color = new Color(color.r, color.g, color.b, 0f);

        var t = new GameObject("Texto", typeof(RectTransform));
        t.transform.SetParent(o.go.transform, false);
        var trt = (RectTransform)t.transform;
        trt.anchorMin = new Vector2(0.1f, 0.2f); trt.anchorMax = new Vector2(0.9f, 0.8f);
        trt.offsetMin = trt.offsetMax = Vector2.zero;
        o.txt = t.AddComponent<TextMeshProUGUI>();
        o.txt.text = text;
        o.txt.fontSize = fontSize;
        o.txt.alignment = TextAlignmentOptions.Center;
        o.txt.color = new Color(1f, 0.9f, 0.7f, 0f);
        o.txt.raycastTarget = false;
        return o;
    }

    public IEnumerator FadeIn(float time) => Fade(0f, 1f, time);
    public IEnumerator FadeOut(float time) => Fade(1f, 0f, time);

    IEnumerator Fade(float from, float to, float time)
    {
        float t = 0f;
        while (t < 1f)
        {
            t += Time.unscaledDeltaTime / time;
            float a = Mathf.Lerp(from, to, Mathf.Clamp01(t));
            img.color = new Color(baseColor.r, baseColor.g, baseColor.b, a);
            txt.color = new Color(1f, 0.9f, 0.7f, a);
            yield return null;
        }
    }

    public void Destroy()
    {
        if (go != null) Object.Destroy(go);
    }
}

// Zona de la cima (la crea el editor al final de la montana). Se activa al llegar el jugador.
public class SummitArena : MonoBehaviour
{
    public Transform startPoint;
    public BossController boss;
    public LavaCrater crater;
    bool begun;

    public void Begin()
    {
        if (begun) return;
        begun = true;
        if (boss == null) boss = Object.FindFirstObjectByType<BossController>(FindObjectsInactive.Include);
        if (boss != null) boss.gameObject.SetActive(true);
        if (RunManager.I != null) RunManager.I.OnSummit();
    }

    PartyManager pm;
    // respaldo por distancia: si el trigger no salta, el jefe aparece al acercarte a la cima
    void Update()
    {
        if (begun || RunManager.I == null || !RunManager.I.Started) return;
        if (pm == null) pm = FindFirstObjectByType<PartyManager>();
        if (pm == null || pm.Current == null) return;
        if (boss == null) boss = FindFirstObjectByType<BossController>(FindObjectsInactive.Include);
        if (boss == null) return;
        Vector3 a = pm.Current.transform.position, b = boss.transform.position;
        if (new Vector2(a.x - b.x, a.z - b.z).magnitude < 40f && Mathf.Abs(a.y - b.y) < 22f) Begin();
    }

    void OnTriggerEnter(Collider other)
    {
        if (other.GetComponentInParent<PlayerMovement>() == null) return;
        if (RunManager.I != null && !RunManager.I.Started) return;
        Begin();
    }
}
