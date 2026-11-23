using TMPro;
using UnityEngine;

// Piezas que el RunManager crea por codigo al generar cada piso.
public static class FB
{
    // Destroy en juego, DestroyImmediate en el editor (los builders tambien usan estas piezas)
    static void Kill(Object o)
    {
        if (Application.isPlaying) Object.Destroy(o); else Object.DestroyImmediate(o);
    }

    public static GameObject Box(string name, Vector3 pos, Vector3 scale, Color col, Transform parent, bool collider = true)
    {
        var go = GameObject.CreatePrimitive(PrimitiveType.Cube);
        go.name = name;
        go.transform.SetParent(parent, false);
        go.transform.localPosition = pos;
        go.transform.localScale = scale;
        go.GetComponent<Renderer>().material.color = col;
        if (!collider) Kill(go.GetComponent<Collider>());
        return go;
    }

    public static GameObject Label(Transform parent, string text, Vector3 pos, float size, Vector2 box)
    {
        var go = new GameObject("Cartel");
        go.transform.SetParent(parent, false);
        go.transform.localPosition = pos;
        go.transform.rotation = Quaternion.Euler(0, 90, 0);   // legible mirando hacia +X
        var t = go.AddComponent<TextMeshPro>();
        t.text = text;
        t.fontSize = size;
        t.alignment = TextAlignmentOptions.Center;
        t.textWrappingMode = TextWrappingModes.Normal;
        t.color = Color.white;
        t.rectTransform.sizeDelta = box;
        return go;
    }

    public static void Shelter(Transform parent, Vector3 f)
    {
        var stone = new Color(0.22f, 0.2f, 0.2f);
        Box("Columna", f + new Vector3(-2f, 1.6f, 0), new Vector3(0.6f, 3.2f, 0.6f), stone, parent);
        Box("Columna", f + new Vector3(2f, 1.6f, 0), new Vector3(0.6f, 3.2f, 0.6f), stone, parent);
        Box("Techo", f + new Vector3(0, 3.4f, 0), new Vector3(5.2f, 0.5f, 3.6f), stone, parent);
    }

    // ---------- salida ----------
    public static FloorExit MakeExit(Transform parent, Vector3 pos)
    {
        var root = new GameObject("Salida");
        root.transform.SetParent(parent, false);
        root.transform.localPosition = pos;

        var stone = new Color(0.25f, 0.25f, 0.3f);
        Box("Pilar", pos + new Vector3(0, 2f, -1.9f), new Vector3(0.6f, 4f, 0.6f), stone, parent);
        Box("Pilar", pos + new Vector3(0, 2f, 1.9f), new Vector3(0.6f, 4f, 0.6f), stone, parent);
        Box("Dintel", pos + new Vector3(0, 4.2f, 0), new Vector3(0.6f, 0.5f, 4.4f), stone, parent);

        var disc = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
        disc.name = "Disco";
        Kill(disc.GetComponent<Collider>());
        disc.transform.SetParent(root.transform, false);
        disc.transform.localPosition = new Vector3(0, 0.04f, 0);
        disc.transform.localScale = new Vector3(3.2f, 0.04f, 3.2f);
        disc.GetComponent<Renderer>().material.color = new Color(0.4f, 0.4f, 0.4f);

        var lg = new GameObject("Luz");
        lg.transform.SetParent(root.transform, false);
        lg.transform.localPosition = new Vector3(0, 2.5f, 0);
        var l = lg.AddComponent<Light>();
        l.type = LightType.Point; l.range = 10f; l.intensity = 6f; l.color = new Color(0.3f, 1f, 0.5f);
        l.enabled = false;

        var bc = root.AddComponent<BoxCollider>();
        bc.isTrigger = true; bc.center = new Vector3(0, 2f, 0); bc.size = new Vector3(3f, 4f, 3.4f);
        var rb = root.AddComponent<Rigidbody>();
        rb.isKinematic = true; rb.useGravity = false;

        var fe = root.AddComponent<FloorExit>();
        fe.disc = disc.GetComponent<Renderer>();
        fe.glow = l;
        return fe;
    }

    // ---------- cofre y altar ----------
    public static void MakeChest(Transform parent, Vector3 pos, int coins)
    {
        GameObject b;
        var model = Resources.Load<GameObject>("Props/Cofre");
        if (model != null)
        {
            b = new GameObject("CofreMonedas");
            b.transform.SetParent(parent, false);
            b.transform.localPosition = pos;
            var bc = b.AddComponent<BoxCollider>();
            bc.center = new Vector3(0f, 0.5f, 0f); bc.size = new Vector3(1.6f, 1f, 1.1f);
            var m = Object.Instantiate(model, b.transform);
            m.transform.localPosition = Vector3.zero;
            m.transform.localRotation = Quaternion.Euler(0f, 270f, 0f);   // mira hacia la entrada del piso
        }
        else
        {
            b = Box("CofreMonedas", pos + new Vector3(0, 0.45f, 0), new Vector3(1.4f, 0.9f, 1f), new Color(0.55f, 0.35f, 0.12f), parent);
            b.AddComponent<ModelSlot>().slotName = "Cofre";
        }
        var c = b.AddComponent<CoinChest>();
        c.coins = coins;
        Label(parent, "Cofre", pos + new Vector3(0, 2f, 0), 4f, new Vector2(4, 1)).AddComponent<ProximityLabel>().radius = 7f;
    }

    public static void MakeAltar(Transform parent, Vector3 pos)
    {
        GameObject b;
        var model = Resources.Load<GameObject>("Props/Altar");
        if (model != null)
        {
            b = new GameObject("AltarReliquias");
            b.transform.SetParent(parent, false);
            b.transform.localPosition = pos;
            var bc = b.AddComponent<BoxCollider>();
            bc.center = new Vector3(0f, 0.6f, 0f); bc.size = new Vector3(1.5f, 1.2f, 1.5f);
            var m = Object.Instantiate(model, b.transform);
            m.transform.localPosition = Vector3.zero;
        }
        else
        {
            b = Box("AltarReliquias", pos + new Vector3(0, 0.55f, 0), new Vector3(1.4f, 1.1f, 1.4f), new Color(0.45f, 0.3f, 0.6f), parent);
            b.AddComponent<ModelSlot>().slotName = "Altar";
        }
        b.AddComponent<BoonAltar>();
        var s = GameObject.CreatePrimitive(PrimitiveType.Sphere);
        s.name = "Reliquia";
        Kill(s.GetComponent<Collider>());
        s.transform.SetParent(parent, false);
        s.transform.localPosition = pos + new Vector3(0, 1.6f, 0);
        s.transform.localScale = Vector3.one * 0.5f;
        s.GetComponent<Renderer>().material.color = new Color(0.8f, 0.5f, 1f);
        Label(parent, "Altar de reliquias", pos + new Vector3(0, 2.8f, 0), 4f, new Vector2(5, 1)).AddComponent<ProximityLabel>().radius = 8f;
    }

    // ---------- puzzles ----------
    // Devuelve cuantas activaciones necesita la compuerta
    public static int BuildPuzzle(int type, Transform parent, Vector3 c, ActivationCounter sink)
    {
        string hint;
        int need = 1;
        switch (type)
        {
            case 0: // cubo -> placa
                hint = "Deja el cubo sobre la placa";
                MakeCube(parent, c + new Vector3(-5f, 0f, 0f));
                MakePlate(parent, c + new Vector3(4f, 0f, 0f), sink);
                break;
            case 1: // dos cubos, dos placas
                hint = "Dos cubos, dos placas";
                MakeCube(parent, c + new Vector3(-5f, 0f, -2f));
                MakeCube(parent, c + new Vector3(-5f, 0f, 2f));
                MakePlate(parent, c + new Vector3(4f, 0f, -3f), sink);
                MakePlate(parent, c + new Vector3(4f, 0f, 3f), sink);
                need = 2;
                break;
            case 2: // lanzar el cubo a una plataforma
                hint = "Lanza el cubo sobre la plataforma";
                MakeCube(parent, c + new Vector3(-5f, 0f, 0f));
                Box("Plataforma", c + new Vector3(4f, 0.75f, 0f), new Vector3(3f, 1.5f, 3f), new Color(0.45f, 0.33f, 0.22f), parent);
                MakePlate(parent, c + new Vector3(4f, 1.5f, 0f), sink);
                break;
            case 3: // cristal (magia)
                hint = "Cristal: dispara magia (Mago)";
                MakeCrystal(parent, c + new Vector3(4f, 0f, 0f), sink);
                break;
            default: // palanca
                hint = "Tira de la palanca";
                MakeLever(parent, c + new Vector3(4f, 0f, 0f), sink);
                break;
        }
        return need;
    }

    static void MakeCube(Transform parent, Vector3 floorPos)
    {
        var go = Box("Cubo", floorPos + new Vector3(0, 0.45f, 0), Vector3.one * 0.8f, new Color(0.3f, 0.5f, 1f), parent);
        go.AddComponent<ModelSlot>().slotName = "Cubo";
        go.AddComponent<PuzzleCube>();   // agrega su Rigidbody
    }

    public static WeightPlate MakePlate(Transform parent, Vector3 topPos, Activatable target)
    {
        var plate = Box("Placa", topPos + new Vector3(0, 0.05f, 0), new Vector3(2.4f, 0.1f, 2.4f), new Color(0.45f, 0.45f, 0.5f), parent);
        Surf.Apply(plate, Surf.Kind.Plate, 2.4f);
        var bc = plate.GetComponent<BoxCollider>();
        bc.isTrigger = true; bc.size = new Vector3(0.9f, 10f, 0.9f); bc.center = new Vector3(0f, 4f, 0f);
        var rb = plate.AddComponent<Rigidbody>();
        rb.isKinematic = true; rb.useGravity = false;
        var wp = plate.AddComponent<WeightPlate>();
        wp.plate = plate.GetComponent<Renderer>();
        wp.targets = new Activatable[] { target };
        return wp;
    }

    public static Crystal MakeCrystal(Transform parent, Vector3 floorPos, Activatable target)
    {
        var ped = Box("Pedestal", floorPos + new Vector3(0, 0.6f, 0), new Vector3(1.4f, 1.2f, 1.4f), new Color(0.3f, 0.3f, 0.32f), parent);
        var c = Box("Cristal", floorPos + new Vector3(0, 2f, 0), new Vector3(1f, 1.6f, 1f), new Color(0.35f, 0.4f, 0.55f), parent);
        c.transform.rotation = Quaternion.Euler(0, 45, 0);
        var lg = new GameObject("Brillo");
        lg.transform.SetParent(c.transform, false);
        lg.transform.localPosition = new Vector3(0, 1.2f, 0);
        var l = lg.AddComponent<Light>();
        l.type = LightType.Point; l.range = 8f; l.intensity = 6f; l.color = new Color(0.3f, 0.9f, 1f);
        var cr = c.AddComponent<Crystal>();
        cr.glow = l;
        cr.targets = new Activatable[] { target };
        try { PropModels.DressCrystal(parent, ped, c, cr); } catch (System.Exception ex) { Debug.LogError("DressCrystal: " + ex); }
        return cr;
    }

    public static Lever MakeLever(Transform parent, Vector3 pos, Activatable target)
    {
        var root = new GameObject("Palanca");
        root.transform.SetParent(parent, false);
        root.transform.localPosition = pos;
        Box("Base", pos + new Vector3(0, 0.2f, 0), new Vector3(0.8f, 0.4f, 0.8f), new Color(0.3f, 0.3f, 0.3f), parent, false);

        var handle = new GameObject("Mango");
        handle.transform.SetParent(root.transform, false);
        handle.transform.localPosition = new Vector3(0, 0.4f, 0);
        var red = new Color(0.85f, 0.15f, 0.15f);

        var bar = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
        Kill(bar.GetComponent<Collider>());
        bar.transform.SetParent(handle.transform, false);
        bar.transform.localPosition = new Vector3(0, 0.5f, 0);
        bar.transform.localScale = new Vector3(0.12f, 0.5f, 0.12f);
        bar.GetComponent<Renderer>().material.color = red;

        var knob = GameObject.CreatePrimitive(PrimitiveType.Sphere);
        Kill(knob.GetComponent<Collider>());
        knob.transform.SetParent(handle.transform, false);
        knob.transform.localPosition = new Vector3(0, 1.0f, 0);
        knob.transform.localScale = Vector3.one * 0.3f;
        knob.GetComponent<Renderer>().material.color = red;

        var col = root.AddComponent<BoxCollider>();
        col.center = new Vector3(0, 0.8f, 0); col.size = new Vector3(0.8f, 1.6f, 0.8f);
        var lever = root.AddComponent<Lever>();
        lever.prompt = "Palanca";
        lever.handle = handle.transform;
        lever.targets = new Activatable[] { target };
        return lever;
    }
}

// Salida del piso: se abre al resolver los puzzles y lleva al siguiente.
public class FloorExit : Activatable
{
    public Renderer disc;
    public Light glow;
    bool open;

    public override void Activate()
    {
        if (open) return;
        open = true;
        Paint(new Color(0.2f, 1f, 0.45f));
        if (glow != null) glow.enabled = true;
        Sfx.Beep(900f, 0.25f);
        HUD.Say("La salida se abre. Cruza el portal");
    }

    public override void Deactivate()
    {
        if (!open) return;
        open = false;
        Paint(new Color(0.4f, 0.4f, 0.4f));
        if (glow != null) glow.enabled = false;
    }

    void Paint(Color c)
    {
        if (disc == null) return;
        var m = disc.material;
        m.color = c;
        m.EnableKeyword("_EMISSION");
        m.SetColor("_EmissionColor", c * 1.5f);
    }

    void OnTriggerEnter(Collider other)
    {
        if (!open) return;
        if (other.GetComponentInParent<PlayerMovement>() == null) return;
        if (RunManager.I != null) RunManager.I.NextFloor();
    }
}
