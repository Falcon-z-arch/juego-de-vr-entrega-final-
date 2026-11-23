using System.Collections;
using UnityEngine;

// Puzzles en equipo: cada piso necesita al HOBBIT (gatera baja) y a un segundo personaje a eleccion:
// el MAGO (cristal a distancia) o el INGENIERO (gancho). Los dos "encargos" se pueden hacer en cualquier orden.

// Deja pasar la activacion una sola vez y no se desactiva (para que cambiar de personaje no deshaga el avance).
public class LatchRelay : Activatable
{
    public Activatable target;
    public string task;                       // que hay que hacer (para el chequeo de la puerta)
    public System.Func<string> detail;        // paso concreto que falta (opcional)
    bool fired;
    public bool Fired => fired;
    public override void Activate()
    {
        if (fired) return;
        fired = true;
        if (target != null) target.Activate();
    }
    public override void Deactivate() { }
}

// Puente que sube desde el suelo sobre la lava.
public class BridgeDeploy : Activatable
{
    public Vector3 from, to;
    bool done;
    public bool Done => done;
    public override void Activate()
    {
        if (done) return;
        done = true;
        Sfx.Beep(500f, 0.3f);
        HUD.Say("Un puente de piedra sube sobre la lava");
        StartCoroutine(Rise());
    }
    IEnumerator Rise()
    {
        float t = 0f;
        while (t < 1f)
        {
            t += Time.deltaTime / 1.6f;
            transform.localPosition = Vector3.Lerp(from, to, Mathf.SmoothStep(0f, 1f, t));
            yield return null;
        }
    }
}

public static class CoopBuilder
{
    public static LatchRelay[] LastLatches;   // los 2 encargos del ultimo puzzle construido
    static readonly Color Stone = new Color(0.3f, 0.3f, 0.34f);

    // Construye un puzzle en equipo dentro de "c" (centro de la terraza). Devuelve las activaciones que necesita la compuerta (2).
    public static int Build(Transform c, ActivationCounter sink)
    {
        int h = Random.Range(0, 2), x = Random.Range(0, 2);
        float s = Random.value < 0.5f ? 1f : -1f;   // lado de la gatera

        var latchH = Latch(c, "EncargoHobbit", sink);
        var latchX = Latch(c, "EncargoAyuda", sink);

        Hut(c, s, h, latchH);
        if (x == 0) Chasm(c, latchX); else Tower(c, latchX);
        latchH.task = h == 0 ? "HOBBIT: agachado (boton Habilidad), entra a la caseta de piedra y tira la palanca del fondo"
                             : "HOBBIT: agachado (boton Habilidad), entra a la caseta de piedra y pisa la placa del fondo";
        LastLatches = new[] { latchH, latchX };

        Hint(c, "PUZZLE EN EQUIPO\nHobbit + (Mago o Ingeniero)", new Vector3(-13f, 5f, 0f), 22f);
        return 2;
    }

    static LatchRelay Latch(Transform c, string name, Activatable target)
    {
        var go = new GameObject(name);
        go.transform.SetParent(c, false);
        var l = go.AddComponent<LatchRelay>();
        l.target = target;
        return l;
    }

    static void Hint(Transform c, string text, Vector3 pos, float radius = 14f)
    {
        // los puzzles ya no explican como se resuelven (solo los tutoriales)
        return;
    }

    static void Glow(GameObject g, Color col)
    {
        var m = g.GetComponent<Renderer>().material;
        m.color = col;
        m.EnableKeyword("_EMISSION");
        m.SetColor("_EmissionColor", col * 1.6f);
    }

    // ---------- Encargo del hobbit: caseta con gatera baja (solo cabe agachado) ----------
    static void Hut(Transform c, float s, int variant, Activatable latch)
    {
        float cx = -12f, cz = s * 11f, fz = cz - s * 3.5f;   // fz = pared de frente (hacia el centro)
        StoneSkin.Apply(FB.Box("CasetaOeste", new Vector3(cx - 3f, 2f, cz), new Vector3(1f, 4f, 7f), Stone, c));
        StoneSkin.Apply(FB.Box("CasetaEste", new Vector3(cx + 3f, 2f, cz), new Vector3(1f, 4f, 7f), Stone, c));
        StoneSkin.Apply(FB.Box("CasetaFondo", new Vector3(cx, 2f, cz + s * 3.5f), new Vector3(7f, 4f, 1f), Stone, c));
        StoneSkin.Apply(FB.Box("CasetaFrenteI", new Vector3(cx - 2.25f, 2f, fz), new Vector3(2.5f, 4f, 1f), Stone, c));
        StoneSkin.Apply(FB.Box("CasetaFrenteD", new Vector3(cx + 2.25f, 2f, fz), new Vector3(2.5f, 4f, 1f), Stone, c));
        StoneSkin.Apply(FB.Box("CasetaDintel", new Vector3(cx, 2.6f, fz), new Vector3(2f, 2.8f, 1f), Stone, c));   // deja una gatera de 1.2 m de alto
        StoneSkin.Apply(FB.Box("CasetaTecho", new Vector3(cx, 4.25f, cz), new Vector3(7.2f, 0.5f, 8f), Stone, c));

        try { PropModels.DressHut(c, cx, cz, s); } catch (System.Exception ex) { Debug.LogError("DressHut: " + ex); }

        var lg = new GameObject("LuzCaseta");
        lg.transform.SetParent(c, false);
        lg.transform.localPosition = new Vector3(cx, 3f, cz);
        var l = lg.AddComponent<Light>();
        l.type = LightType.Point; l.range = 9f; l.intensity = 5f; l.color = new Color(1f, 0.8f, 0.4f);

        if (variant == 0)
        {
            FB.MakeLever(c, new Vector3(cx, 0f, cz + s * 2f), latch);
            Hint(c, "GATERA\nSolo el Hobbit agachado cabe.\nDentro hay una palanca", new Vector3(cx, 4.9f, fz - s * 1.5f));
        }
        else
        {
            var wp = FB.MakePlate(c, new Vector3(cx, 0f, cz + s * 1.5f), latch);
            wp.cubeOnly = false;
            Hint(c, "GATERA\nSolo el Hobbit agachado cabe.\nPisa la placa del fondo", new Vector3(cx, 4.9f, fz - s * 1.5f));
        }
    }

    // ---------- Encargo de ayuda A: foso de lava ----------
    // Mago: dispara al cristal y sube un puente. Ingeniero: gancho al pilar y cruza volando. Luego se tira la palanca del otro lado.
    static void Chasm(Transform c, Activatable latch)
    {
        var lava = FB.Box("Lava", new Vector3(5f, 0.02f, 0f), new Vector3(12f, 0.04f, 34f), Color.red, c, false);
        Glow(lava, new Color(1f, 0.35f, 0.05f));

        var kz = FB.Box("LavaMortal", new Vector3(5f, -0.3f, 0f), new Vector3(12f, 1f, 34f), Color.black, c);
        kz.GetComponent<BoxCollider>().isTrigger = true;
        kz.GetComponent<Renderer>().enabled = false;
        var rb = kz.AddComponent<Rigidbody>();
        rb.isKinematic = true; rb.useGravity = false;
        kz.AddComponent<KillZone>();

        var plank = FB.Box("PuenteLava", new Vector3(5f, -1.0f, 0f), new Vector3(12.4f, 0.4f, 4f), new Color(0.45f, 0.4f, 0.35f), c);
        var br = plank.AddComponent<BridgeDeploy>();
        Surf.Apply(plank, Surf.Kind.Wood, 2f);
        br.from = new Vector3(5f, -1.0f, 0f);
        br.to = new Vector3(5f, 0.08f, 0f);

        FB.MakeCrystal(c, new Vector3(-3f, 0f, 5f), br);
        Surf.Apply(FB.Box("PilarGancho", new Vector3(12.5f, 2f, 0f), new Vector3(1.2f, 4f, 1.2f), Stone, c), Surf.Kind.Stone, 2f);
        var lv = FB.MakeLever(c, new Vector3(15.5f, 0f, -4f), latch);
        lv.interactDistance = 4f;
        var lt = (LatchRelay)latch;
        lt.task = "AYUDA: cruza el foso de lava y tira la palanca del otro lado";
        lt.detail = () => br.Done
            ? "AYUDA: el puente ya subio; cruza y tira la palanca del otro lado de la lava"
            : "AYUDA: Mago, dispara al cristal del foso para subir el puente (o Ingeniero, gancho al pilar de piedra y cruza); luego tira la palanca del otro lado";

        Hint(c, "FOSO DE LAVA\nMago: dispara al cristal\nIngeniero: gancho al pilar", new Vector3(-3f, 4f, -5f));
    }

    // ---------- Encargo de ayuda B: torre alta ----------
    // Mago: dispara al cristal de arriba. Ingeniero: gancho a la torre y tira la palanca de la cima.
    static void Tower(Transform c, Activatable latch)
    {
        Surf.Apply(FB.Box("Torre", new Vector3(4f, 1.6f, 0f), new Vector3(5f, 3.2f, 5f), new Color(0.4f, 0.36f, 0.3f), c), Surf.Kind.Stone, 2.5f);
        var lv = FB.MakeLever(c, new Vector3(2.8f, 3.2f, 1.2f), latch);
        lv.interactDistance = 2.4f;
        ((LatchRelay)latch).task = "AYUDA: Mago, dispara al cristal de la cima de la torre; o Ingeniero, gancho a la torre y tira la palanca de arriba";
        FB.MakeCrystal(c, new Vector3(5.3f, 3.2f, -1.3f), latch);
        Hint(c, "TORRE\nMago: dispara al cristal de arriba\nIngeniero: gancho y palanca de la cima", new Vector3(4f, 7.5f, -4f), 18f);
    }
}

// Chequeo de la puerta del piso: tocala (o acercate) y dice que falta para abrirla.
// Si los dos encargos estan hechos y la puerta sigue cerrada, la abre.
public class DoorCheck : Interactable
{
    public LatchRelay[] latches;
    public MountainDoor door;
    public bool primary = true;          // false = el boton junto a la puerta
    public Renderer buttonLamp;          // luz del boton (roja = falta, verde = listo)
    public TMPro.TMP_Text board;         // texto sobre el boton
    float nextAuto, nextBoard;
    PartyManager party;

    void Awake() { prompt = "Puerta"; interactDistance = 8f; }

    public override void Interact(PlayerMovement who) { Report(); }

    bool AllDone()
    {
        if (latches == null) return false;
        foreach (var l in latches) if (l == null || !l.Fired) return false;
        return true;
    }

    float Dist()
    {
        if (party == null) party = FindFirstObjectByType<PartyManager>();
        if (party == null || party.Current == null || door == null) return 999f;
        Vector3 a = party.Current.transform.position, b = door.transform.position;
        return new Vector2(a.x - b.x, a.z - b.z).magnitude;
    }

    // lista de encargos siempre visible (IMGUI) cuando estas en el piso de esta puerta
    void OnGUI()
    {
        if (!primary) return;
        if (door == null || door.IsOpen || latches == null || Dist() > 32f) return;
        int fs = Mathf.Max(14, Screen.height / 42);
        var st = new GUIStyle(GUI.skin.label) { fontSize = fs, richText = true };
        float y = Screen.height * 0.30f;
        GUI.Label(new Rect(14, y, Screen.width * 0.6f, fs * 1.6f), "<b>PUERTA DEL PISO</b>", st);
        for (int i = 0; i < latches.Length; i++)
        {
            var l = latches[i];
            if (l == null) continue;
            string t = i == 0 ? "Hobbit (caseta)" : "Ayuda (Mago/Ingeniero)";
            GUI.Label(new Rect(14, y + (i + 1) * fs * 1.5f, Screen.width * 0.6f, fs * 1.6f), (l.Fired ? "<color=#66ff66>[OK] " : "<color=#ff8866>[falta] ") + t + "</color>", st);
        }
    }

    int lastDone = -1;

    void Update()
    {
        if (!primary) { ButtonUpdate(); return; }
        if (door == null || door.IsOpen) return;
        if (latches != null)
        {
            int n = 0;
            foreach (var l in latches) if (l != null && l.Fired) n++;
            if (n != lastDone)
            {
                if (lastDone >= 0 && n > lastDone && n < latches.Length) { HUD.Say("La puerta baja un poco (" + n + "/" + latches.Length + ")"); Sfx.Beep(300f, 0.25f); }
                lastDone = n;
                door.SetProgress((float)n / latches.Length);
            }
        }
        if (AllDone()) { HUD.Say("Todo resuelto: abriendo la puerta"); door.Activate(); return; }
        if (Time.unscaledTime < nextAuto) return;
        if (Dist() > 4.5f) return;
        nextAuto = Time.unscaledTime + 9f;
        Report();
    }

    void ButtonUpdate()
    {
        if (door == null) return;
        if (Time.unscaledTime >= nextBoard)
        {
            nextBoard = Time.unscaledTime + 0.25f;
            bool open = door.IsOpen;
            var sb = new System.Text.StringBuilder();
            sb.Append("<b>PUERTA</b>\n");
            if (open) sb.Append("<color=#66ff66>ABIERTA</color>");
            else if (latches != null)
                for (int i = 0; i < latches.Length; i++)
                {
                    bool ok = latches[i] != null && latches[i].Fired;
                    sb.Append(ok ? "<color=#66ff66>[OK] " : "<color=#ff8866>[falta] ").Append(i == 0 ? "Hobbit" : "Ayuda").Append("</color>\n");
                }
            if (board != null)
            {
                board.text = sb.ToString();
                var cam = Camera.main;
                if (cam != null)
                {
                    Vector3 d = board.transform.position - cam.transform.position; d.y = 0f;
                    if (d.sqrMagnitude > 0.01f) board.transform.rotation = Quaternion.LookRotation(d);
                }
            }
            if (buttonLamp != null)
            {
                Color c = (open || AllDone()) ? new Color(0.2f, 1f, 0.4f) : new Color(1f, 0.25f, 0.15f);
                var m = buttonLamp.material; m.color = c; m.EnableKeyword("_EMISSION"); m.SetColor("_EmissionColor", c * 2f);
            }
        }
        var kb = UnityEngine.InputSystem.Keyboard.current;
        if (kb != null && kb.eKey.wasPressedThisFrame && Dist() < 6f) Report();
    }

    public void Report()
    {
        if (door == null) return;
        if (door.IsOpen) { HUD.Say("La puerta ya esta abierta. ¡Sube!"); return; }
        var miss = new System.Collections.Generic.List<string>();
        int done = 0;
        if (latches != null)
            foreach (var l in latches)
            {
                if (l == null) continue;
                if (l.Fired) { done++; continue; }
                miss.Add(l.detail != null ? l.detail() : l.task);
            }
        if (miss.Count == 0)
        {
            HUD.Say("Todo resuelto: abriendo la puerta");
            door.Activate();
            return;
        }
        HUD.Say("Para abrir la puerta falta (" + done + "/" + (latches != null ? latches.Length : 2) + "):  " + string.Join("   |   ", miss));
    }
}

// Sigue la posicion vertical de otro objeto (decoracion que baja con la puerta).
public class FollowDoor : MonoBehaviour
{
    public Transform target;
    Vector3 off;
    void Start() { if (target != null) off = transform.position - target.position; }
    void LateUpdate() { if (target != null) transform.position = target.position + off; }
}

// Decora la puerta del piso (marco de piedra, bandas doradas, sello brillante, antorchas) y le pone un boton al lado.
public static class DoorDecor
{
    static GameObject Glowing(string name, Vector3 lp, Vector3 size, Color c, Transform parent, bool collider, float k)
    {
        var g = FB.Box(name, lp, size, c, parent, collider);
        var m = g.GetComponent<Renderer>().material;
        m.EnableKeyword("_EMISSION"); m.SetColor("_EmissionColor", c * k);
        return g;
    }

    public static void Build(Transform content, MountainDoor door, LatchRelay[] latches)
    {
        if (door == null) return;
        Vector3 dp = door.transform.position;                 // centro de la puerta (mundo)
        float floor = dp.y - door.transform.lossyScale.y * 0.5f;
        float halfW = door.transform.lossyScale.z * 0.5f;      // medio ancho de la puerta
        Func_Local f = w => content.InverseTransformPoint(w);

        // puerta de piedra oscura
        try { Surf.Apply(door.gameObject, Surf.Kind.Door, 3f); } catch { }
        if (door.rend != null) door.rend.material.color = new Color(1f, 0.92f, 0.85f);

        var stone = new Color(0.34f, 0.33f, 0.38f);
        var gold = new Color(0.95f, 0.72f, 0.25f);
        var cyan = new Color(0.35f, 0.9f, 1f);

        // marco fijo: dos pilares y dintel
        foreach (float sgn in new[] { -1f, 1f })
        {
            var pil = FB.Box("PilarPuerta", f(new Vector3(dp.x, floor + 3.8f, dp.z + sgn * (halfW + 0.7f))), new Vector3(2.2f, 7.6f, 1.4f), stone, content);
            try { StoneSkin.Apply(pil); } catch { }
            Glowing("DetalleOro", f(new Vector3(dp.x - 1.15f, floor + 5.6f, dp.z + sgn * (halfW + 0.7f))), new Vector3(0.12f, 0.5f, 1.0f), gold, content, false, 0.6f);
            // antorcha
            var fl = Glowing("Llama", f(new Vector3(dp.x - 1.2f, floor + 4.6f, dp.z + sgn * (halfW + 0.7f))), new Vector3(0.35f, 0.5f, 0.35f), new Color(1f, 0.55f, 0.1f), content, false, 3f);
            var lg = new GameObject("LuzAntorcha"); lg.transform.SetParent(content, false); lg.transform.localPosition = fl.transform.localPosition + new Vector3(-0.4f, 0.3f, 0f);
            var l = lg.AddComponent<Light>(); l.type = LightType.Point; l.range = 9f; l.intensity = 6f; l.color = new Color(1f, 0.6f, 0.25f);
        }
        var lint = FB.Box("DintelPuerta", f(new Vector3(dp.x, floor + 7.4f, dp.z)), new Vector3(2.2f, 1.2f, halfW * 2f + 3.2f), stone, content);
        try { StoneSkin.Apply(lint); } catch { }
        Glowing("OroDintel", f(new Vector3(dp.x - 1.15f, floor + 7.4f, dp.z)), new Vector3(0.12f, 0.25f, halfW * 2f + 2.4f), gold, content, false, 0.6f);

        // decoracion que baja con la puerta
        var deco = new GameObject("PuertaDeco");
        deco.transform.SetParent(content, false);
        deco.transform.position = dp;
        var fd = deco.AddComponent<FollowDoor>(); fd.target = door.transform;
        foreach (float y in new[] { -2.4f, 0f, 2.4f })
            Glowing("Banda", new Vector3(0f, y, 0f), new Vector3(1.5f, 0.3f, halfW * 2f + 0.1f), gold, deco.transform, false, 0.5f);
        var seal = Glowing("Sello", new Vector3(-0.2f, 0f, 0f), new Vector3(1.5f, 1.8f, 1.8f), cyan, deco.transform, false, 2.2f);
        seal.transform.localRotation = Quaternion.Euler(45f, 0f, 0f);
        var core = FB.Box("SelloNucleo", new Vector3(-0.3f, 0f, 0f), new Vector3(1.5f, 0.9f, 0.9f), new Color(0.1f, 0.1f, 0.15f), deco.transform, false);
        core.transform.localRotation = Quaternion.Euler(45f, 0f, 0f);
        var sl = new GameObject("LuzSello"); sl.transform.SetParent(deco.transform, false); sl.transform.localPosition = new Vector3(-1.5f, 0f, 0f);
        var sli = sl.AddComponent<Light>(); sli.type = LightType.Point; sli.range = 10f; sli.intensity = 5f; sli.color = cyan;

        // chequeo al tocar la puerta
        var dc = door.gameObject.AddComponent<DoorCheck>();
        dc.door = door; dc.latches = latches; dc.primary = true;

        // boton junto a la puerta (lado del jugador)
        float bz = dp.z + halfW - 1.5f;
        Vector3 bp = new Vector3(dp.x - 2.8f, floor, bz);
        var ped = FB.Box("PedestalBoton", f(bp + new Vector3(0f, 0.55f, 0f)), new Vector3(0.8f, 1.1f, 0.8f), stone, content);
        try { StoneSkin.Apply(ped); } catch { }
        var lamp = Glowing("Boton", f(bp + new Vector3(0f, 1.2f, 0f)), new Vector3(0.55f, 0.16f, 0.55f), new Color(1f, 0.25f, 0.15f), content, false, 2f);
        var tgo = new GameObject("TextoBoton");
        tgo.transform.SetParent(content, false);
        tgo.transform.localPosition = f(bp + new Vector3(0f, 2.5f, 0f));
        var tmp = tgo.AddComponent<TMPro.TextMeshPro>();
        tmp.fontSize = 3.2f; tmp.alignment = TMPro.TextAlignmentOptions.Center; tmp.color = Color.white; tmp.textWrappingMode = TMPro.TextWrappingModes.NoWrap;
        tmp.rectTransform.sizeDelta = new Vector2(6f, 3f);
        var bc = ped.AddComponent<DoorCheck>();
        bc.door = door; bc.latches = latches; bc.primary = false; bc.buttonLamp = lamp.GetComponent<Renderer>(); bc.board = tmp;
        bc.prompt = "Boton de la puerta"; bc.interactDistance = 8f;
    }

    delegate Vector3 Func_Local(Vector3 w);
}
