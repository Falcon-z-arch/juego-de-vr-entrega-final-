using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;
using UnityEngine.XR;

// HALL DE ACCESO de ASCUA (escena "Hall"). Se arma entero al ejecutar (primitivas + texturas procedurales), igual que el resto del juego.
// - Bienvenida, historia, objetivo y controles en paneles de piedra.
// - El Ascua (reloj de arena) flota en el centro. El portal violeta es el punto de acceso: se cruza caminando,
//   o se apunta con el control derecho (VR) / mouse y se aprieta el gatillo / clic.
// - Funciona con casco Quest (OpenXR) y en plano (WASD + clic derecho para mirar) para probar sin lentes.
public class HallController : MonoBehaviour
{
    const float PlazaHalf = 12f;
    static readonly Vector3 PortalPos = new Vector3(0f, 0f, 10.5f);

    Transform rig, head, handL, handR, ray;
    Camera cam;
    bool vr, starting, hover;
    InputAction hmdPos, hmdRot, lPos, lRot, rPos, rRot, lStick, rStick, rTrig, lTrig;
    float yaw, pitch, snapCd, clock;
    Transform portalSwirl, portalDisc, hourglass;
    Light portalLight;
    AudioSource hum;

    void Start()
    {
        vr = XRSettings.isDeviceActive || PlayerPrefs.GetInt("modoVR", 0) == 1;
        Time.timeScale = 1f;
        BuildEnvironment();
        BuildPlaza();
        BuildPortal();
        BuildPanels();
        BuildRig();
        foreach (var n in new[] { "Plaza" }) { var g = GameObject.Find(n); if (g != null) QuestPerf.Combine(g); }
        MusicPlayer.Play("menu", 0.3f);
        StartCoroutine(Welcome());
    }

    IEnumerator Welcome()
    {
        yield return new WaitForSeconds(1f);
        Sfx.Beep(440f, 0.15f); yield return new WaitForSeconds(0.18f);
        Sfx.Beep(660f, 0.2f);
    }

    // ------------------------------------------------------------------ entorno
    void BuildEnvironment()
    {
        RenderSettings.ambientMode = UnityEngine.Rendering.AmbientMode.Flat;
        RenderSettings.ambientLight = new Color(0.16f, 0.12f, 0.2f);
        RenderSettings.fog = true;
        RenderSettings.fogMode = FogMode.Linear;
        RenderSettings.fogColor = new Color(0.05f, 0.03f, 0.09f);
        RenderSettings.fogStartDistance = 22f;
        RenderSettings.fogEndDistance = 70f;

        foreach (var l in FindObjectsByType<Light>(FindObjectsSortMode.None))
            if (l.type == LightType.Directional) { l.color = new Color(0.45f, 0.5f, 0.85f); l.intensity = 0.5f; l.shadows = LightShadows.None; l.transform.rotation = Quaternion.Euler(50f, -30f, 0f); }

        // estrellas (esferas emisivas, sin sombras ni colliders)
        var rnd = new System.Random(5);
        var star = PropModels.Mat(new Color(1f, 0.95f, 0.8f), 2.5f);
        var root = new GameObject("Estrellas").transform;
        for (int i = 0; i < 70; i++)
        {
            float a = (float)rnd.NextDouble() * Mathf.PI * 2f, e = 0.15f + (float)rnd.NextDouble() * 1.1f;
            var p = new Vector3(Mathf.Cos(a) * Mathf.Cos(e), Mathf.Sin(e), Mathf.Sin(a) * Mathf.Cos(e)) * 90f;
            var s = Prim(PrimitiveType.Sphere, "Estrella", p, Vector3.one * (0.3f + (float)rnd.NextDouble() * 0.5f), star, root, false);
        }
        // volcan lejano con brillo de lava
        var rock = PropModels.Mat(new Color(0.12f, 0.08f, 0.09f), 0f, 0.1f);
        var lava = PropModels.Mat(new Color(1f, 0.4f, 0.08f), 2.5f);
        var v = Prim(PrimitiveType.Cylinder, "Volcan", new Vector3(0f, 14f, 85f), new Vector3(60f, 14f, 60f), rock, root, false);
        Prim(PrimitiveType.Cylinder, "Lava", new Vector3(0f, 28.05f, 85f), new Vector3(16f, 0.1f, 16f), lava, root, false);
        var lg = new GameObject("LuzVolcan"); lg.transform.SetParent(root, false); lg.transform.position = new Vector3(0f, 32f, 80f);
        var ll = lg.AddComponent<Light>(); ll.type = LightType.Point; ll.range = 60f; ll.intensity = 30f; ll.color = new Color(1f, 0.45f, 0.15f); ll.shadows = LightShadows.None;
    }

    void BuildPlaza()
    {
        var root = new GameObject("Plaza").transform;
        var floor = Prim(PrimitiveType.Cube, "Suelo", new Vector3(0f, -0.5f, 0f), new Vector3(PlazaHalf * 2f, 1f, PlazaHalf * 2f), null, root, true);
        Surf.Apply(floor, Surf.Kind.Stone, 3f);
        floor.GetComponent<Renderer>().sharedMaterial = floor.GetComponent<Renderer>().sharedMaterial;

        // camino rojo hasta el portal + circulo de runas
        var red = PropModels.Mat(new Color(0.45f, 0.08f, 0.08f), 0.2f, 0.2f);
        Prim(PrimitiveType.Cube, "Alfombra", new Vector3(0f, 0.02f, 3.5f), new Vector3(3f, 0.04f, 15f), red, root, false);
        var gold = PropModels.Mat(new Color(0.95f, 0.75f, 0.25f), 0.5f, 0.7f, 0.8f);
        var rune = PropModels.Mat(new Color(0.6f, 0.4f, 1f), 1.8f);
        for (int i = 0; i < 24; i++)
        {
            float a = i / 24f * Mathf.PI * 2f;
            Prim(PrimitiveType.Cube, "Runa", new Vector3(Mathf.Cos(a) * 3.2f, 0.03f, Mathf.Sin(a) * 3.2f), new Vector3(0.35f, 0.04f, 0.12f), rune, root, false).transform.rotation = Quaternion.Euler(0f, -a * Mathf.Rad2Deg + 90f, 0f);
        }
        // pedestal + Ascua flotante
        var ped = Prim(PrimitiveType.Cylinder, "Pedestal", new Vector3(0f, 0.45f, 0f), new Vector3(1.8f, 0.45f, 1.8f), PropModels.Mat(new Color(0.3f, 0.3f, 0.34f), 0f, 0.2f), root, true);
        Prim(PrimitiveType.Cylinder, "PedestalOro", new Vector3(0f, 0.92f, 0f), new Vector3(1.9f, 0.04f, 1.9f), gold, root, false);
        var hg = new GameObject("Ascua"); hg.transform.SetParent(root, false); hg.transform.position = new Vector3(0f, 2.3f, 0f); hg.transform.localScale = Vector3.one * 0.9f;
        try { hg.AddComponent<ArtifactVisual>(); } catch (System.Exception ex) { Debug.LogError("Hall Ascua: " + ex.Message); }
        hourglass = hg.transform;
        var al = new GameObject("LuzAscua"); al.transform.SetParent(root, false); al.transform.position = new Vector3(0f, 2.5f, 0f);
        var l = al.AddComponent<Light>(); l.type = LightType.Point; l.range = 12f; l.intensity = 8f; l.color = new Color(0.85f, 0.55f, 1f); l.shadows = LightShadows.None;

        // columnas con antorchas alrededor (solo 4 luces reales: el resto es emision)
        var stone = PropModels.Mat(new Color(0.34f, 0.33f, 0.38f), 0f, 0.2f);
        var flame = PropModels.Mat(new Color(1f, 0.55f, 0.1f), 3f);
        int torchLights = 0;
        for (int i = 0; i < 12; i++)
        {
            float a = i / 12f * Mathf.PI * 2f;
            Vector3 p = new Vector3(Mathf.Cos(a), 0f, Mathf.Sin(a)) * (PlazaHalf - 0.6f);
            if (Vector3.Distance(p, PortalPos) < 4f) continue;
            var col = Prim(PrimitiveType.Cube, "Columna", p + Vector3.up * 2.5f, new Vector3(1.1f, 5f, 1.1f), null, root, true);
            Surf.Apply(col, Surf.Kind.Stone, 2f);
            Prim(PrimitiveType.Cube, "Capitel", p + Vector3.up * 5.2f, new Vector3(1.5f, 0.4f, 1.5f), stone, root, false);
            Vector3 inward = -p.normalized;
            var fl = Prim(PrimitiveType.Sphere, "Llama", p + Vector3.up * 3.2f + inward * 0.7f, new Vector3(0.3f, 0.45f, 0.3f), flame, root, false);
            Prim(PrimitiveType.Cube, "Soporte", p + Vector3.up * 2.9f + inward * 0.6f, new Vector3(0.12f, 0.5f, 0.12f), gold, root, false);
            if (torchLights < 4 && i % 3 == 0)
            {
                torchLights++;
                var tl = fl.AddComponent<Light>(); tl.type = LightType.Point; tl.range = 9f; tl.intensity = 5f; tl.color = new Color(1f, 0.6f, 0.25f); tl.shadows = LightShadows.None;
            }
        }
        // murallas bajas para no caerse al vacio
        foreach (float s in new[] { -1f, 1f })
        {
            var w1 = Prim(PrimitiveType.Cube, "Muro", new Vector3(s * (PlazaHalf + 0.5f), 1f, 0f), new Vector3(1f, 2f, PlazaHalf * 2f + 2f), null, root, true);
            Surf.Apply(w1, Surf.Kind.Stone, 3f);
            var w2 = Prim(PrimitiveType.Cube, "Muro", new Vector3(0f, 1f, s * (PlazaHalf + 0.5f)), new Vector3(PlazaHalf * 2f + 2f, 2f, 1f), null, root, true);
            Surf.Apply(w2, Surf.Kind.Stone, 3f);
        }
    }

    void BuildPortal()
    {
        var root = new GameObject("Portal").transform;
        root.position = PortalPos;
        var gold = PropModels.Mat(new Color(0.95f, 0.75f, 0.25f), 0.5f, 0.7f, 0.8f);
        var glow = PropModels.Mat(new Color(0.6f, 0.3f, 1f), 2.6f);
        var stone = PropModels.Mat(new Color(0.3f, 0.3f, 0.34f), 0f, 0.2f);
        // arco de piedra
        foreach (float s in new[] { -1f, 1f })
        {
            var pil = Prim(PrimitiveType.Cube, "PilarPortal", root.position + new Vector3(s * 3.1f, 2.6f, 0f), new Vector3(1.4f, 5.2f, 1.4f), null, root, true);
            Surf.Apply(pil, Surf.Kind.Stone, 2f);
        }
        var din = Prim(PrimitiveType.Cube, "DintelPortal", root.position + new Vector3(0f, 5.6f, 0f), new Vector3(7.8f, 1.2f, 1.4f), null, root, true);
        Surf.Apply(din, Surf.Kind.Stone, 2f);
        // anillo luminoso
        for (int i = 0; i < 20; i++)
        {
            float a = i / 20f * Mathf.PI * 2f;
            Prim(PrimitiveType.Cube, "Anillo", root.position + new Vector3(Mathf.Cos(a) * 2.5f, 2.6f + Mathf.Sin(a) * 2.6f, 0f), new Vector3(0.25f, 0.5f, 0.2f), (i % 2 == 0) ? gold : glow, root, false).transform.rotation = Quaternion.Euler(0f, 0f, a * Mathf.Rad2Deg);
        }
        // disco (activa el portal al tocarlo / apuntarlo) y remolino
        portalDisc = Prim(PrimitiveType.Cylinder, "PortalDisco", root.position + new Vector3(0f, 2.6f, 0f), new Vector3(4.6f, 0.05f, 4.6f), PropModels.Mat(new Color(0.35f, 0.15f, 0.8f), 1.4f, 0.9f), root, true).transform;
        portalDisc.rotation = Quaternion.Euler(90f, 0f, 0f);
        var col = portalDisc.GetComponent<Collider>(); col.isTrigger = true;
        var sw = new GameObject("Remolino"); sw.transform.SetParent(root, false); sw.transform.position = root.position + new Vector3(0f, 2.6f, -0.05f);
        for (int i = 0; i < 8; i++)
        {
            float a = i / 8f * Mathf.PI * 2f;
            Prim(PrimitiveType.Cube, "Brazo", sw.transform.position + new Vector3(Mathf.Cos(a) * 1.2f, Mathf.Sin(a) * 1.2f, 0f), new Vector3(1.4f, 0.12f, 0.05f), glow, sw.transform, false).transform.rotation = Quaternion.Euler(0f, 0f, a * Mathf.Rad2Deg + 35f);
        }
        portalSwirl = sw.transform;
        var lg = new GameObject("LuzPortal"); lg.transform.SetParent(root, false); lg.transform.position = root.position + new Vector3(0f, 2.6f, -1.5f);
        portalLight = lg.AddComponent<Light>(); portalLight.type = LightType.Point; portalLight.range = 14f; portalLight.intensity = 10f; portalLight.color = new Color(0.65f, 0.35f, 1f); portalLight.shadows = LightShadows.None;
        // cartel
        var t = Text(root, "ENTRAR A LA COMARCA", root.position + new Vector3(0f, 6.6f, 0f), new Vector2(9f, 1.2f), 6f, new Color(1f, 0.85f, 0.4f), TextAlignmentOptions.Center);
        t.transform.rotation = Quaternion.LookRotation(Vector3.forward);
        var t2 = Text(root, "Cruza el portal o apuntalo y aprieta el gatillo", root.position + new Vector3(0f, 0.4f, -1.2f), new Vector2(8f, 0.7f), 3f, new Color(0.9f, 0.85f, 1f), TextAlignmentOptions.Center);
        t2.transform.rotation = Quaternion.LookRotation(Vector3.forward);

        // zumbido del portal (sonido ambiente 3D, generado por codigo)
        hum = root.gameObject.AddComponent<AudioSource>();
        hum.clip = MakeHum(); hum.loop = true; hum.spatialBlend = 1f; hum.minDistance = 3f; hum.maxDistance = 22f; hum.volume = 0.5f; hum.Play();
    }

    static AudioClip MakeHum()
    {
        int sr = 22050, n = sr * 2;
        var d = new float[n];
        for (int i = 0; i < n; i++)
        {
            float t = i / (float)sr;
            d[i] = 0.25f * Mathf.Sin(2 * Mathf.PI * 110f * t) + 0.15f * Mathf.Sin(2 * Mathf.PI * 165f * t) + 0.08f * Mathf.Sin(2 * Mathf.PI * 220f * t + Mathf.Sin(t * 3f));
        }
        var c = AudioClip.Create("PortalHum", n, 1, sr, false);
        c.SetData(d, 0);
        return c;
    }

    void BuildPanels()
    {
        var root = new GameObject("Paneles").transform;
        Panel(root, new Vector3(-8.5f, 2.2f, -1f), "HISTORIA",
            "El Cronarca detuvo el tiempo en la Comarca.\nEl Ascua, un reloj de arena, pesa en quien lo carga y lo corrompe.\n\nUn hobbit, un mago, un ingeniero y un peleador deben subir la montana de Karnak: cuatro pisos de puzzles y fuego hasta la cima.");
        Panel(root, new Vector3(8.5f, 2.2f, -1f), "CONTROLES (VR)",
            "Joystick izq: moverse   |   Joystick der: girar\nA: saltar   X: cambiar personaje\nY: agacharse (hobbit)   B: interactuar\nGrip: agarrar cubos / escalar\nGatillo: gancho (ingeniero) / magia (mago)\nMago: dos gatillos = arco, separa las manos para cargar");
        Panel(root, new Vector3(-7f, 2.2f, 7f), "OBJETIVO",
            "Sube los 4 pisos resolviendo puzzles en equipo (Hobbit + Mago o Ingeniero).\nEsquiva las bolas de fuego, vence al Cronarca y lleva el Ascua a la lava.\n\nEn la aldea, los 3 vendedores mejoran tu equipo.");
        Panel(root, new Vector3(7f, 2.2f, 7f), "CONSEJOS",
            "El Ascua corrompe a quien la lleva: cambia de personaje a tiempo.\nLa puerta de cada piso baja un poco con cada encargo.\nLa magia rompe el fuego y activa los cristales.");
    }

    void Panel(Transform parent, Vector3 pos, string title, string body)
    {
        var g = new GameObject("Panel_" + title); g.transform.SetParent(parent, false);
        g.transform.position = pos;
        Vector3 away = new Vector3(pos.x, 0f, pos.z - 2f);       // el texto mira hacia el centro de la plaza
        g.transform.rotation = Quaternion.LookRotation(new Vector3(away.x, 0f, away.z).normalized);
        var back = Prim(PrimitiveType.Cube, "Fondo", pos, new Vector3(4.6f, 2.9f, 0.15f), null, g.transform, true);
        back.transform.rotation = g.transform.rotation;
        Surf.Apply(back, Surf.Kind.Stone, 2f);
        var gold = PropModels.Mat(new Color(0.95f, 0.75f, 0.25f), 0.6f, 0.7f, 0.8f);
        foreach (var o in new[] { new Vector3(0, 1.5f, 0), new Vector3(0, -1.5f, 0) })
            Prim(PrimitiveType.Cube, "Marco", pos + g.transform.rotation * o, new Vector3(4.8f, 0.12f, 0.2f), gold, g.transform, false).transform.rotation = g.transform.rotation;
        foreach (var o in new[] { new Vector3(2.4f, 0, 0), new Vector3(-2.4f, 0, 0) })
            Prim(PrimitiveType.Cube, "Marco", pos + g.transform.rotation * o, new Vector3(0.12f, 3.1f, 0.2f), gold, g.transform, false).transform.rotation = g.transform.rotation;
        var t = Text(g.transform, title, pos + g.transform.rotation * new Vector3(0f, 1.05f, -0.1f), new Vector2(4.2f, 0.6f), 4f, new Color(1f, 0.8f, 0.3f), TextAlignmentOptions.Center);
        t.fontStyle = FontStyles.Bold; t.transform.rotation = g.transform.rotation;
        var b = Text(g.transform, body, pos + g.transform.rotation * new Vector3(0f, -0.3f, -0.1f), new Vector2(4.2f, 2f), 2.2f, new Color(0.95f, 0.93f, 0.9f), TextAlignmentOptions.Center);
        b.transform.rotation = g.transform.rotation;
    }

    // ------------------------------------------------------------------ jugador
    void BuildRig()
    {
        rig = new GameObject("RigHall").transform;
        rig.position = new Vector3(0f, 0f, -8f);
        cam = Camera.main;
        if (cam == null) { var cg = new GameObject("Main Camera", typeof(Camera), typeof(AudioListener)); cg.tag = "MainCamera"; cam = cg.GetComponent<Camera>(); }
        cam.clearFlags = CameraClearFlags.SolidColor;
        cam.backgroundColor = new Color(0.03f, 0.02f, 0.06f);
        cam.nearClipPlane = 0.1f; cam.farClipPlane = 220f;
        head = cam.transform;
        head.SetParent(rig, false);
        head.localPosition = new Vector3(0f, 1.7f, 0f);
        head.localRotation = Quaternion.identity;
        if (vr)
        {
            hmdPos = new InputAction("hp", InputActionType.Value, "<XRHMD>/centerEyePosition");
            hmdRot = new InputAction("hr", InputActionType.Value, "<XRHMD>/centerEyeRotation");
            lPos = new InputAction("lp", InputActionType.Value, "<XRController>{LeftHand}/devicePosition");
            lRot = new InputAction("lr", InputActionType.Value, "<XRController>{LeftHand}/deviceRotation");
            rPos = new InputAction("rp", InputActionType.Value, "<XRController>{RightHand}/devicePosition");
            rRot = new InputAction("rr", InputActionType.Value, "<XRController>{RightHand}/deviceRotation");
            lStick = new InputAction("ls", InputActionType.Value, "<XRController>{LeftHand}/thumbstick");
            rStick = new InputAction("rs", InputActionType.Value, "<XRController>{RightHand}/thumbstick");
            rTrig = new InputAction("rt", InputActionType.Button, "<XRController>{RightHand}/trigger");
            lTrig = new InputAction("lt", InputActionType.Button, "<XRController>{LeftHand}/trigger");
            foreach (var a in new[] { hmdPos, hmdRot, lPos, lRot, rPos, rRot, lStick, rStick, rTrig, lTrig }) a.Enable();
            handL = MakeHand(true); handR = MakeHand(false);
        }
    }

    Transform MakeHand(bool left)
    {
        var h = new GameObject(left ? "ManoIzq" : "ManoDer").transform;
        h.SetParent(rig, false);
        var body = Prim(PrimitiveType.Sphere, "Control", Vector3.zero, Vector3.one * 0.09f, PropModels.Mat(new Color(0.15f, 0.12f, 0.2f), 0f, 0.5f), h, false);
        Prim(PrimitiveType.Sphere, "Punta", new Vector3(0f, 0f, 0.07f), Vector3.one * 0.03f, PropModels.Mat(new Color(0.5f, 0.9f, 1f), 2.5f), h, false);
        if (!left)
        {
            var r = Prim(PrimitiveType.Cube, "Rayo", new Vector3(0f, 0f, 2.5f), new Vector3(0.006f, 0.006f, 5f), PropModels.Mat(new Color(0.5f, 0.9f, 1f), 2f), h, false);
            ray = r.transform;
        }
        return h;
    }

    void Update()
    {
        clock += Time.deltaTime;
        if (portalSwirl != null) portalSwirl.Rotate(0f, 0f, 60f * Time.deltaTime);
        if (hourglass != null) hourglass.position = new Vector3(0f, 2.3f + Mathf.Sin(clock * 1.2f) * 0.12f, 0f);
        if (portalLight != null) portalLight.intensity = 9f + Mathf.Sin(clock * 3f) * 2f + (hover ? 6f : 0f);
        if (starting || rig == null) return;
        if (vr) UpdateVR(); else UpdateFlat();

        // cruzar el portal caminando
        Vector3 hp = head.position;
        if (Mathf.Abs(hp.x - PortalPos.x) < 2f && Mathf.Abs(hp.z - PortalPos.z) < 1.2f) StartGame();
    }

    void ClampRig()
    {
        var p = rig.position;
        p.x = Mathf.Clamp(p.x, -PlazaHalf + 1f, PlazaHalf - 1f);
        p.z = Mathf.Clamp(p.z, -PlazaHalf + 1f, PortalPos.z + 1.5f);
        p.y = 0f;
        rig.position = p;
    }

    void UpdateVR()
    {
        if (head != null)
        {
            var hp = hmdPos.ReadValue<Vector3>();
            if (hp != Vector3.zero) { head.localPosition = hp + new Vector3(0f, 0f, 0f); head.localRotation = hmdRot.ReadValue<Quaternion>(); }
        }
        if (handL != null) { handL.localPosition = lPos.ReadValue<Vector3>(); handL.localRotation = lRot.ReadValue<Quaternion>(); }
        if (handR != null) { handR.localPosition = rPos.ReadValue<Vector3>(); handR.localRotation = rRot.ReadValue<Quaternion>(); }

        Vector2 mv = lStick.ReadValue<Vector2>();
        if (mv.sqrMagnitude > 0.04f)
        {
            Vector3 f = head.forward; f.y = 0f; f.Normalize();
            Vector3 r = head.right; r.y = 0f; r.Normalize();
            rig.position += (f * mv.y + r * mv.x) * 2.5f * Time.deltaTime;
            ClampRig();
        }
        snapCd -= Time.deltaTime;
        float tx = rStick.ReadValue<Vector2>().x;
        if (snapCd <= 0f && Mathf.Abs(tx) > 0.7f)
        {
            snapCd = 0.3f;
            Vector3 pivot = head.position;
            rig.RotateAround(pivot, Vector3.up, Mathf.Sign(tx) * 30f);
        }
        // apuntar con el control derecho al portal
        bool over = false;
        if (handR != null && Physics.Raycast(handR.position, handR.forward, out var hit, 20f, ~0, QueryTriggerInteraction.Collide))
            over = hit.collider.transform == portalDisc;
        SetHover(over);
        if (over && (rTrig.WasPressedThisFrame() || lTrig.WasPressedThisFrame())) StartGame();
    }

    void UpdateFlat()
    {
        var kb = Keyboard.current; var ms = Mouse.current;
        if (kb != null)
        {
            Vector2 mv = Vector2.zero;
            if (kb.wKey.isPressed || kb.upArrowKey.isPressed) mv.y += 1f;
            if (kb.sKey.isPressed || kb.downArrowKey.isPressed) mv.y -= 1f;
            if (kb.dKey.isPressed) mv.x += 1f;
            if (kb.aKey.isPressed) mv.x -= 1f;
            Vector3 f = Quaternion.Euler(0f, yaw, 0f) * Vector3.forward, r = Quaternion.Euler(0f, yaw, 0f) * Vector3.right;
            rig.position += (f * mv.y + r * mv.x) * 3.5f * Time.deltaTime;
            ClampRig();
        }
        if (ms != null && ms.rightButton.isPressed)
        {
            Vector2 d = ms.delta.ReadValue();
            yaw += d.x * 0.12f; pitch = Mathf.Clamp(pitch - d.y * 0.12f, -60f, 60f);
        }
        head.localRotation = Quaternion.Euler(pitch, yaw, 0f);
        // clic / toque sobre el portal
        var ptr = Pointer.current;
        if (ptr != null)
        {
            var ray = cam.ScreenPointToRay(ptr.position.ReadValue());
            bool over = Physics.Raycast(ray, out var hit, 40f, ~0, QueryTriggerInteraction.Collide) && hit.collider.transform == portalDisc;
            SetHover(over);
            if (over && ptr.press.wasPressedThisFrame) StartGame();
        }
    }

    void SetHover(bool over)
    {
        if (over && !hover) Sfx.Beep(880f, 0.06f);
        hover = over;
        if (portalDisc != null) portalDisc.localScale = new Vector3(4.6f * (hover ? 1.04f : 1f), 0.05f, 4.6f * (hover ? 1.04f : 1f));
    }

    void StartGame()
    {
        if (starting) return;
        starting = true;
        if (vr) PlayerPrefs.SetInt("modoVR", 1);
        PlayerPrefs.Save();
        StartCoroutine(Go());
    }

    IEnumerator Go()
    {
        for (int i = 0; i < 5; i++) { Sfx.Beep(440f + i * 140f, 0.12f); yield return new WaitForSeconds(0.12f); }
        float t = 0f;
        while (t < 1.2f)
        {
            t += Time.deltaTime;
            if (portalLight != null) portalLight.intensity += 40f * Time.deltaTime;
            yield return null;
        }
        IntroCinematic.Reset();
        if (Application.CanStreamedLevelBeLoaded("Comarca")) SceneManager.LoadScene("Comarca");
        else if (Application.CanStreamedLevelBeLoaded("Menu")) SceneManager.LoadScene("Menu");
        else Debug.LogWarning("Hall: falta la escena Comarca en Build Profiles.");
    }

    // ------------------------------------------------------------------ utilidades
    static GameObject Prim(PrimitiveType type, string name, Vector3 pos, Vector3 scale, Material mat, Transform parent, bool collider)
    {
        var g = GameObject.CreatePrimitive(type);
        g.name = name;
        if (!collider) { var c = g.GetComponent<Collider>(); if (c != null) Destroy(c); }
        if (parent != null) g.transform.SetParent(parent, true);
        g.transform.position = pos;
        g.transform.localScale = scale;
        if (mat != null) g.GetComponent<Renderer>().sharedMaterial = mat;
        var r = g.GetComponent<Renderer>();
        if (r != null) r.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
        return g;
    }

    static TextMeshPro Text(Transform parent, string s, Vector3 worldPos, Vector2 size, float fs, Color c, TextAlignmentOptions al)
    {
        var g = new GameObject("Texto");
        g.transform.SetParent(parent, true);
        g.transform.position = worldPos;
        var t = g.AddComponent<TextMeshPro>();
        t.text = s; t.fontSize = fs; t.color = c; t.alignment = al;
        t.enableWordWrapping = true;
        t.rectTransform.sizeDelta = size;
        return t;
    }
}
