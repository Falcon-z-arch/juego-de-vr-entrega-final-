using UnityEngine;
using static ZonaPruebasBuilder;

// Escenario del modo historia (roguelike): la escena Volcan solo trae la CIMA ya armada (lejos, en y = 300).
// Los 4 pisos de la mazmorra los genera RunManager por codigo al empezar cada partida.
internal static class VolcanBuilder
{
    // punto a distancia r del centro del volcan, en el angulo deg, a altura y
    static Vector3 P(float r, float deg, float y)
    {
        float a = deg * Mathf.Deg2Rad;
        return new Vector3(r * Mathf.Cos(a), y, r * Mathf.Sin(a));
    }

    internal static LavaCrater Build(out Vector3 start)
    {
        start = new Vector3(-17f, 0f, 0f);   // inicio del piso 1 (RunManager genera la sala)

        RenderSettings.fog = true;
        RenderSettings.fogMode = FogMode.Exponential;
        RenderSettings.fogColor = new Color(0.22f, 0.12f, 0.1f);
        RenderSettings.fogDensity = 0.006f;
        RenderSettings.ambientMode = UnityEngine.Rendering.AmbientMode.Flat;
        RenderSettings.ambientLight = new Color(0.45f, 0.36f, 0.34f);

        var crater = BuildSummit();

        // ---------- Zonas de caida ----------
        MakeKillZone("ZonaCaida", -25f);
        MakeKillZone("ZonaCaidaCima", 270f);
        return crater;
    }

    // Arma la cima en y = 300 centrada en el origen (ComarcaBuilder la mueve al final de la montana)
    internal static LavaCrater BuildSummit()
    {
        float Y = 300f;
        var stone = Mat("VPiedra", new Color(0.22f, 0.2f, 0.2f));
        Cylinder("CimaSuelo", new Vector3(0, Y - 0.5f, 0), 24f, 1f, Mat("VCima", new Color(0.2f, 0.16f, 0.15f)));
        if (!DungeonKit.Available)
            for (int i = 0; i < 16; i++)
                Prim(PrimitiveType.Cube, "PilarCima", P(23.5f, i * 22.5f, Y + 2f), new Vector3(1.2f, 4f, 1.2f), stone);

        var lava = Cylinder("Lava", new Vector3(0, Y + 0.05f, 0), 4.5f, 0.1f, Mat("VLava", new Color(1f, 0.35f, 0.05f), true));
        var crater = lava.AddComponent<LavaCrater>();
        crater.prompt = "Crater";
        crater.interactDistance = 14f;
        var lavaLightGO = new GameObject("LuzLava");
        lavaLightGO.transform.position = new Vector3(0, Y + 4f, 0);
        var lavaLight = lavaLightGO.AddComponent<Light>();
        lavaLight.type = LightType.Point; lavaLight.range = 45f; lavaLight.intensity = 15f; lavaLight.color = new Color(1f, 0.45f, 0.1f);

        // jefe: reloj flotante (empieza apagado; SummitArena lo activa)
        var boss = new GameObject("Cronarca");
        boss.transform.position = new Vector3(0, Y + 4f, 0);
        var body = Prim(PrimitiveType.Sphere, "Cuerpo", Vector3.zero, Vector3.one * 4.2f, Mat("CronarcaCuerpo", new Color(0.7f, 0.5f, 0.2f), true), boss.transform, false);
        var face = Prim(PrimitiveType.Cylinder, "Esfera", new Vector3(0, 0, -1.9f), new Vector3(3.4f, 0.15f, 3.4f), Mat("CronarcaCara", new Color(0.95f, 0.92f, 0.8f)), boss.transform, false);
        face.transform.localRotation = Quaternion.Euler(90f, 0f, 0f);
        Prim(PrimitiveType.Cube, "ManoReloj", new Vector3(0, 0.7f, -2.05f), new Vector3(0.15f, 1.4f, 0.1f), Mat("CronarcaMano", Color.black), boss.transform, false);
        var sc = boss.AddComponent<SphereCollider>();
        sc.radius = 2.2f;
        var bc = boss.AddComponent<BossController>();
        bc.floorY = Y;
        boss.AddComponent<ModelSlot>().slotName = "Cronarca";
        boss.SetActive(false);

        // cubos para herir al jefe (lanzalos contra el)
        VolcanBuilder.MakeCube("CuboJefe_1", P(15f, 150f, Y));
        VolcanBuilder.MakeCube("CuboJefe_2", P(15f, 210f, Y));
        VolcanBuilder.MakeCube("CuboJefe_3", P(15f, 330f, Y));
        Shelter(P(18f, 70f, Y));
        Shelter(P(18f, 290f, Y));

        EvilKit.Summit(Y);   // cima de reino malvado (si hay packs de dungeon)

        var startGO = new GameObject("InicioCima");
        startGO.transform.position = new Vector3(-20f, Y, 0f);

        var arena = new GameObject("CimaArena").AddComponent<SummitArena>();
        arena.startPoint = startGO.transform;
        arena.boss = bc;
        arena.crater = crater;

        return crater;
    }

    static void MakeKillZone(string name, float y)
    {
        var kz = Prim(PrimitiveType.Cube, name, new Vector3(0, y, 0), new Vector3(400f, 1f, 400f), Mat("Caida", Color.black));
        kz.GetComponent<BoxCollider>().isTrigger = true;
        kz.GetComponent<Renderer>().enabled = false;
        Kinematic(kz);
        kz.AddComponent<KillZone>();
    }

    // ------------------------------------------------------------------ Piezas
    internal static void Block(string name, Vector3 center, Vector3 size, string matName)
    {
        Prim(PrimitiveType.Cube, name, center, size, Mat(matName, new Color(0.45f, 0.33f, 0.22f)));
    }

    internal static PuzzleCube MakeCube(string name, Vector3 floorPos)
    {
        var go = Prim(PrimitiveType.Cube, name, floorPos + new Vector3(0, 0.45f, 0), Vector3.one * 0.8f, Mat("Cubo", new Color(0.3f, 0.5f, 1f), true));
        go.AddComponent<Rigidbody>();
        go.AddComponent<ModelSlot>().slotName = "Cubo";
        return go.AddComponent<PuzzleCube>();
    }

    // Placa de presion en el piso (topPos = punto de la superficie donde se apoya)
    internal static WeightPlate MakePlate(string name, Vector3 topPos, params Activatable[] targets)
    {
        var plate = Prim(PrimitiveType.Cube, name, topPos + new Vector3(0, 0.05f, 0), new Vector3(2.5f, 0.1f, 2.5f), Mat("PlacaCubo", Color.gray));
        var box = plate.GetComponent<BoxCollider>();
        box.isTrigger = true; box.size = new Vector3(0.9f, 10f, 0.9f); box.center = new Vector3(0f, 4f, 0f);
        Kinematic(plate);
        var wp = plate.AddComponent<WeightPlate>();
        wp.plate = plate.GetComponent<Renderer>();
        wp.targets = targets;
        return wp;
    }

    internal static CheckpointBeacon MakeBeacon(string name, Vector3 floorPos)
    {
        var root = new GameObject(name);
        root.transform.position = floorPos;
        Prim(PrimitiveType.Cylinder, "Poste", new Vector3(0, 1.2f, 0), new Vector3(0.25f, 1.2f, 0.25f), Mat("BalizaPoste", new Color(0.25f, 0.25f, 0.28f)), root.transform, false);
        var lamp = Prim(PrimitiveType.Sphere, "Lampara", new Vector3(0, 2.6f, 0), Vector3.one * 0.6f, Mat("BalizaLampara", new Color(0.3f, 0.3f, 0.3f)), root.transform, false);
        var lightGO = new GameObject("Luz");
        lightGO.transform.SetParent(root.transform, false);
        lightGO.transform.localPosition = new Vector3(0, 2.6f, 0);
        var l = lightGO.AddComponent<Light>();
        l.type = LightType.Point; l.range = 12f; l.intensity = 8f; l.color = new Color(1f, 0.9f, 0.3f);
        l.enabled = false;
        var b = root.AddComponent<CheckpointBeacon>();
        b.lamp = lamp.GetComponent<Renderer>();
        b.glow = l;
        return b;
    }

    // Fila de agarres de escalada en espiral sobre la pared de radio R (6 m de alto)
    static void AddHolds(float R, float deg, float baseY)
    {
        var mat = Mat("Agarre", new Color(0.9f, 0.8f, 0.25f), true);
        float step = 0.5f / R * Mathf.Rad2Deg;
        for (int k = 0; k < 7; k++)
        {
            float y = baseY + 0.8f + k * 0.8f;
            float a = deg + (k % 2 == 0 ? 0f : step * 1.2f);
            bool top = k == 6;
            var h = Prim(PrimitiveType.Cube, top ? "Agarre_borde" : "Agarre", P(R + 0.2f, a, y), new Vector3(0.35f, 0.18f, 0.35f), mat);
            h.GetComponent<BoxCollider>().isTrigger = true;
            var hold = h.AddComponent<ClimbHold>();
            if (top)
            {
                Vector3 inward = -new Vector3(h.transform.position.x, 0f, h.transform.position.z).normalized;
                hold.mantle = true;
                hold.mantleOffset = inward * 1.2f + Vector3.up * (baseY + 7f - y);
            }
        }
    }

    internal static BoostPad MakePad(string name, Vector3 floorPos)
    {
        var root = new GameObject(name);
        root.transform.position = floorPos;
        var disc = Prim(PrimitiveType.Cylinder, "Disco", new Vector3(0, 0.06f, 0), new Vector3(3f, 0.06f, 3f), Mat("VRespiradero", new Color(0.25f, 0.25f, 0.25f)), root.transform, false);

        var lightGO = new GameObject("Luz");
        lightGO.transform.SetParent(root.transform, false);
        lightGO.transform.localPosition = new Vector3(0, 1.5f, 0);
        var l = lightGO.AddComponent<Light>();
        l.type = LightType.Point; l.range = 10f; l.intensity = 8f; l.color = new Color(1f, 0.5f, 0.1f);

        var trigger = root.AddComponent<BoxCollider>();
        trigger.isTrigger = true; trigger.center = new Vector3(0, 1.2f, 0); trigger.size = new Vector3(3f, 2.4f, 3f);
        Kinematic(root);

        var bp = root.AddComponent<BoostPad>();
        bp.pad = disc.GetComponent<Renderer>();
        bp.glow = l;
        Vector3 dir = -floorPos; dir.y = 0f;
        bp.horizontalDirection = dir.normalized;   // empuja hacia el centro del volcan
        return bp;
    }

    // Techo de piedra sobre dos columnas: las rocas rebotan en el y puedes resguardarte debajo
    internal static void Shelter(Vector3 f)
    {
        var stone = Mat("VPiedra", new Color(0.22f, 0.2f, 0.2f));
        Prim(PrimitiveType.Cube, "Columna", f + new Vector3(-2f, 1.6f, 0), new Vector3(0.6f, 3.2f, 0.6f), stone);
        Prim(PrimitiveType.Cube, "Columna", f + new Vector3(2f, 1.6f, 0), new Vector3(0.6f, 3.2f, 0.6f), stone);
        Prim(PrimitiveType.Cube, "Techo", f + new Vector3(0, 3.4f, 0), new Vector3(5.2f, 0.5f, 3.6f), stone);
    }
}
