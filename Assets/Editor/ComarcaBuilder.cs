using UnityEditor;
using UnityEngine;
using static ZonaPruebasBuilder;

// La Comarca: aldea de inicio con vendedores (mejoras permanentes con las monedas ganadas)
// y la Puerta de Karnak para empezar una partida. El camino va hacia +X.
internal static class ComarcaBuilder
{
    internal static LavaCrater Build(out Vector3 start)
    {
        start = new Vector3(-48f, 0f, 0f);

        RenderSettings.fog = false;
        RenderSettings.ambientMode = UnityEngine.Rendering.AmbientMode.Flat;
        RenderSettings.ambientLight = new Color(0.62f, 0.62f, 0.58f);

        // ---------- Terreno y camino ----------
        EvilKit.Skin(Prim(PrimitiveType.Cube, "Pasto", new Vector3(0, -0.5f, 0), new Vector3(150, 1, 84), Mat("CPasto", new Color(0.32f, 0.5f, 0.25f))), 4f, false, EvilKit.GrassMat());
        Prim(PrimitiveType.Cube, "Camino", new Vector3(-3, 0.02f, 0), new Vector3(100, 0.04f, 7), Mat("CCamino", new Color(0.74f, 0.72f, 0.67f)), null, false);

        var wall = Mat("CMuroPiedra", new Color(0.45f, 0.45f, 0.48f));
        Prim(PrimitiveType.Cube, "Valla_N", new Vector3(0, 1f, 42f), new Vector3(150, 2, 1), wall);
        Prim(PrimitiveType.Cube, "Valla_S", new Vector3(0, 1f, -42f), new Vector3(150, 2, 1), wall);
        // valla este con un hueco de 12 m: ahi empieza la subida
        Prim(PrimitiveType.Cube, "Valla_E1", new Vector3(75f, 1f, 24f), new Vector3(1, 2, 36), wall);
        Prim(PrimitiveType.Cube, "Valla_E2", new Vector3(75f, 1f, -24f), new Vector3(1, 2, 36), wall);
        Prim(PrimitiveType.Cube, "Valla_O", new Vector3(-75f, 1f, 0), new Vector3(1, 2, 84), wall);

        // seguridad: suelo extra invisible y muros invisibles altos alrededor de la aldea
        var base2 = Prim(PrimitiveType.Cube, "SueloBase", new Vector3(-10f, -1.05f, 0f), new Vector3(220f, 1f, 200f), Mat("CPasto", new Color(0.32f, 0.5f, 0.25f)));
        base2.GetComponent<Renderer>().enabled = false;
        foreach (var w in new[] { new Vector3(-76f, 10f, 0f), new Vector3(0f, 10f, 43f), new Vector3(0f, 10f, -43f) })
        {
            bool side = Mathf.Abs(w.x) > 50f;
            var inv = Prim(PrimitiveType.Cube, "MuroInvisible", w, side ? new Vector3(2f, 20f, 90f) : new Vector3(152f, 20f, 2f), wall);
            inv.GetComponent<Renderer>().enabled = false;
        }

        Label("LA COMARCA", new Vector3(-40f, 6f, 0f)).AddComponent<ProximityLabel>().radius = 22f;
        Label("Gasta tus monedas en los puestos. Al fondo, la Puerta de Karnak", new Vector3(-40f, 3.6f, 0f)).AddComponent<ProximityLabel>().radius = 22f;

        // ---------- Casas ----------
        HouseAuto(-30f, 15f, 7f, 6f, new Color(0.75f, 0.6f, 0.45f), false, "plaster");
        HouseAuto(-8f, -16f, 8f, 6f, new Color(0.7f, 0.55f, 0.4f), true, "brick");
        HouseAuto(10f, 16f, 7f, 6f, new Color(0.8f, 0.65f, 0.5f), false, "brick");
        HouseAuto(30f, -15f, 8f, 6f, new Color(0.72f, 0.58f, 0.42f), true, "plaster");
        HouseAuto(-30f, -17f, 6f, 5f, new Color(0.78f, 0.62f, 0.48f), true, "brick");

        // ---------- Puestos de vendedores ----------
        // tiendas pegadas a las casas (extensiones): herreria, laboratorio y mercado
        Stall("Herrero", "HERRERIA\nArmadura y relevo ardiente", new Vector3(-1.95f, 0f, -14.5f), 0f, new Color(0.55f, 0.2f, 0.15f));
        Stall("Alquimista", "ALQUIMIA\nFrascos y descanso", new Vector3(3.95f, 0f, 14.5f), 180f, new Color(0.3f, 0.2f, 0.5f));
        Stall("Mercader", "MERCADO\nBotas y gancho", new Vector3(36.05f, 0f, -13.5f), 0f, new Color(0.2f, 0.4f, 0.5f));

        if (MedievalKit.Available) MedievalKit.Props();

        // ---------- Arboles (siempre fuera del camino y las casas) ----------
        var rnd = new System.Random(7);
        var trunk = Mat("CTronco", new Color(0.35f, 0.24f, 0.14f));
        var leaf = Mat("CHoja", new Color(0.2f, 0.42f, 0.18f));
        for (int i = 0; i < (NatureKit.Available || CitadelKit.Enabled ? 0 : 28); i++)
        {
            float x = (float)(rnd.NextDouble() * 120 - 62);
            float z = (float)(rnd.NextDouble() * 18 + 24) * (rnd.Next(2) == 0 ? -1 : 1);
            float h = (float)(rnd.NextDouble() * 1.5 + 2);
            Prim(PrimitiveType.Cylinder, "Tronco", new Vector3(x, h * 0.5f, z), new Vector3(0.7f, h * 0.5f, 0.7f), trunk);
            Prim(PrimitiveType.Sphere, "Copa", new Vector3(x, h + 1.4f, z), Vector3.one * (3f + (float)rnd.NextDouble()), leaf, null, false);
        }

        // ---------- Puerta de Karnak ----------
        var stone = Mat("CPiedraPuerta", new Color(0.3f, 0.28f, 0.3f));
        Prim(PrimitiveType.Cube, "PilarPuerta", new Vector3(62f, 3.5f, -4.5f), new Vector3(1.6f, 7f, 1.6f), stone);
        Prim(PrimitiveType.Cube, "PilarPuerta", new Vector3(62f, 3.5f, 4.5f), new Vector3(1.6f, 7f, 1.6f), stone);
        Prim(PrimitiveType.Cube, "DintelPuerta", new Vector3(62f, 7.5f, 0f), new Vector3(1.6f, 1.2f, 11.5f), stone);

        var gate = new GameObject("PuertaKarnak");
        gate.transform.position = new Vector3(62f, 0f, 0f);
        var bc = gate.AddComponent<BoxCollider>();
        bc.center = new Vector3(0, 3.2f, 0); bc.size = new Vector3(1.2f, 6.4f, 7.4f);
        var rg = gate.AddComponent<RunGate>();
        var wood = Mat("CMaderaPuerta", new Color(0.2f, 0.12f, 0.08f));
        var iron = Mat("CHierroPuerta", new Color(0.12f, 0.12f, 0.14f));
        var seal = Mat("CSelloPuerta", new Color(1f, 0.15f, 0.08f), true);
        var leaves = new System.Collections.Generic.List<Transform>();
        foreach (float sg in new[] { 1f, -1f })
        {
            var piv = new GameObject("HojaPivote").transform;
            piv.SetParent(gate.transform, false);
            piv.localPosition = new Vector3(0f, 0f, sg * 3.7f);
            Prim(PrimitiveType.Cube, "HojaPuerta", new Vector3(0f, 3.2f, -sg * 1.85f), new Vector3(0.5f, 6.4f, 3.7f), wood, piv, true);
            foreach (float by in new[] { 1.2f, 3.2f, 5.2f })
                Prim(PrimitiveType.Cube, "BandaHierro", new Vector3(-0.28f, by, -sg * 1.85f), new Vector3(0.1f, 0.35f, 3.6f), iron, piv, false);
            Prim(PrimitiveType.Cube, "SelloRojo", new Vector3(-0.3f, 3.2f, -sg * 0.35f), new Vector3(0.08f, 1.6f, 0.35f), seal, piv, false);
            leaves.Add(piv);
        }
        rg.leaves = leaves.ToArray();
        var lg = new GameObject("LuzPuerta");
        lg.transform.position = new Vector3(59f, 4f, 0f);
        var l = lg.AddComponent<Light>();
        l.type = LightType.Point; l.range = 25f; l.intensity = 18f; l.color = new Color(1f, 0.5f, 0.15f);
        Label("PUERTA DE KARNAK\nAl abrirla empieza el bombardeo", new Vector3(60f, 9.5f, 0f)).AddComponent<ProximityLabel>().radius = 30f;

        SkyKit.Apply();
        CitadelKit.Build();
        if (NatureKit.Available) DecorateVillage();
        UalKit.Villagers();
        return BuildMountain();
    }

    // Naturaleza descuidada: la aldea esta abandonada, la maleza se come todo
    static void DecorateVillage()
    {
        var root = new GameObject("Naturaleza").transform;
        var r = new System.Random(21);
        Vector3 spawn = new Vector3(-48f, 0f, 0f);
        System.Func<Vector3, bool> notPath = p => Mathf.Abs(p.z) < 5.5f || (p - spawn).sqrMagnitude < 64f || p.x > 56f;
        System.Func<Vector3, bool> offPath = p => Mathf.Abs(p.z) < 4.2f || (p - spawn).sqrMagnitude < 36f || p.x > 56f;

        // bosque de fondo (a los lados, lejos del camino)
        if (!CitadelKit.Enabled) NatureKit.Scatter(root, r, 26, -70f, 70f, 22f, 40f, 0f, NatureKit.Trees, 1.0f, 1.5f, 1.6f, "trunk", notPath);
        if (!CitadelKit.Enabled) NatureKit.Scatter(root, r, 26, -70f, 70f, -40f, -22f, 0f, NatureKit.Trees, 1.0f, 1.5f, 1.6f, "trunk", notPath);
        if (!CitadelKit.Enabled) NatureKit.Scatter(root, r, 10, -70f, 70f, 12f, 40f, 0f, NatureKit.Pines, 1.0f, 1.4f, 1.4f, "trunk", notPath);
        if (!CitadelKit.Enabled) NatureKit.Scatter(root, r, 10, -70f, 70f, -40f, -12f, 0f, NatureKit.Pines, 1.0f, 1.4f, 1.4f, "trunk", notPath);
        // arboles muertos (abandono) cerca de las casas y el camino
        if (!CitadelKit.Enabled) NatureKit.Scatter(root, r, 14, -70f, 70f, -40f, 40f, 0f, NatureKit.Dead, 0.9f, 1.3f, 1.4f, "trunk", notPath);
        // maleza, pasto, flores y rocas por toda la aldea (fuera del camino)
        NatureKit.Scatter(root, r, 45, -72f, 72f, -22f, 22f, 0f, NatureKit.Bushes, 0.9f, 1.5f, 0.8f, null, notPath);
        NatureKit.Scatter(root, r, 900, -72f, 72f, -40f, 40f, 0f, NatureKit.Grass, 0.9f, 2.0f, 0.2f, null, offPath);
        NatureKit.Scatter(root, r, 130, -72f, 72f, -40f, 40f, 0f, NatureKit.Flowers, 0.9f, 1.8f, 0.3f, null, offPath);
        NatureKit.Scatter(root, r, 30, -72f, 72f, -22f, 22f, 0f, NatureKit.Rocks, 0.8f, 2.2f, 0.8f, "box", notPath);

        // props RPG olvidados: barriles, escudo y espada clavada, cofre roto
        if (NatureKit.RpgAvailable)
        {
            var sw = NatureKit.Place(root, true, "Sword", new Vector3(-40f, 0.6f, 6f), 30f, 0.38f);
            if (sw != null) sw.transform.rotation = Quaternion.Euler(8f, 30f, 6f) * Quaternion.identity;
            var sh = NatureKit.Place(root, true, "Shield", new Vector3(-36f, 0f, -5.8f), 200f, 0.4f, "box");
            if (sh != null) sh.transform.rotation = Quaternion.Euler(-70f, 200f, 0f);
            NatureKit.Place(root, true, "KnightHelmet", new Vector3(30f, 0f, 6.5f), 140f, 0.3f);
            NatureKit.Place(root, true, "WoodenStaff", new Vector3(-52f, 0.1f, 8f), 0f, 0.4f);
        }
        MarkStatic(root);
    }

    internal static void MarkStatic(Transform root)
    {
        foreach (var t in root.GetComponentsInChildren<Transform>())
            GameObjectUtility.SetStaticEditorFlags(t.gameObject, StaticEditorFlags.BatchingStatic);
    }

    // ------------------------------------------------------------------ MONTANA (continua, hacia +X)
    // 4 terrazas con puerta + rampas + cima. Los puzzles los llena RunManager al abrir la puerta de Karnak.
    const float RampLen = 26f, RampW = 12f, TerrW = 34f, Rise = 12f;
    internal const float TerrLen = 60f;   // largo de cada piso (antes 36): espacio para enemigos y armas

    static LavaCrater BuildMountain()
    {
        string[] names = { "Bosque Susurrante", "Laberinto de Raices", "Paso Helado", "Ruinas Antiguas" };
        Color[] floors = { new Color(0.27f, 0.46f, 0.22f), new Color(0.36f, 0.25f, 0.15f), new Color(0.72f, 0.86f, 0.94f), new Color(0.5f, 0.5f, 0.52f) };
        Color[] walls = { new Color(0.2f, 0.33f, 0.18f), new Color(0.25f, 0.17f, 0.1f), new Color(0.5f, 0.65f, 0.78f), new Color(0.33f, 0.33f, 0.36f) };
        var rock = Mat("MRoca", new Color(0.4f, 0.34f, 0.3f));
        var rnd = new System.Random(11);
        var zones = new MountainZone[4];

        float xr = 75f;
        for (int k = 1; k <= 4; k++)
        {
            float h0 = Rise * (k - 1), h1 = Rise * k;
            Ramp(xr, h0, h1, rock);
            float xt = xr + RampLen, xe = xt + TerrLen, cx = xt + TerrLen * 0.5f;
            var fm = Mat("MPiso" + k, floors[k - 1]);
            var wm = Mat("MMuro" + k, walls[k - 1]);

            // terraza + muros (con hueco de 12 m a cada lado para la rampa y la puerta)
            EvilKit.Skin(Prim(PrimitiveType.Cube, "Terraza_" + k, new Vector3(cx, h1 * 0.5f - 0.5f, 0), new Vector3(TerrLen, h1 + 1f, TerrW), fm), 3.5f, false, EvilKit.FloorMat("P" + k, floors[k - 1], 20 + k));
            EvilKit.Skin(Prim(PrimitiveType.Cube, "Muro_N" + k, new Vector3(cx, h1 + 3f, TerrW * 0.5f + 0.5f), new Vector3(TerrLen + 2f, 6f, 1f), wm), 4f);
            EvilKit.Skin(Prim(PrimitiveType.Cube, "Muro_S" + k, new Vector3(cx, h1 + 3f, -TerrW * 0.5f - 0.5f), new Vector3(TerrLen + 2f, 6f, 1f), wm), 4f);
            foreach (float wx in new[] { xt, xe })
                foreach (float sz in new[] { 1f, -1f })
                    EvilKit.Skin(Prim(PrimitiveType.Cube, "MuroHueco" + k, new Vector3(wx, h1 + 3f, sz * 11.5f), new Vector3(1f, 6f, 11f), wm), 4f);

            // puerta del fondo
            var door = Prim(PrimitiveType.Cube, "Puerta_Zona" + k, new Vector3(xe, h1 + 3f, 0f), new Vector3(1.2f, 6f, RampW), Mat("MPuerta", new Color(0.75f, 0.2f, 0.15f), true));
            Kinematic(door);
            var md = door.AddComponent<MountainDoor>();
            md.rend = door.GetComponent<Renderer>();
            md.checkpointAfter = new Vector3(xe + 2f, h1 + 2.2f, 0f);

            // zona (detecta que el jugador entro) + cartel
            var zgo = new GameObject("Zona_" + k);
            zgo.transform.position = new Vector3(cx, h1, 0f);
            var tc = zgo.AddComponent<BoxCollider>();
            tc.isTrigger = true; tc.center = new Vector3(0, 6f, 0); tc.size = new Vector3(TerrLen - 3f, 12f, TerrW - 2f);
            Kinematic(zgo);
            var mz = zgo.AddComponent<MountainZone>();
            mz.index = k; mz.title = names[k - 1]; mz.door = md;
            zones[k - 1] = mz;
            Label("PISO " + k + ": " + names[k - 1].ToUpper(), new Vector3(xt + 2f, h1 + 4.5f, 0f)).AddComponent<ProximityLabel>().radius = 16f;

            Decor(k, xt, h1, rnd);
            xr = xe;
        }

        // rampa final + cima
        Ramp(xr, Rise * 4, Rise * 5, rock);
        float H5 = Rise * 5, sx = xr + RampLen + 24f;
        var before = new System.Collections.Generic.HashSet<GameObject>(UnityEngine.SceneManagement.SceneManager.GetActiveScene().GetRootGameObjects());
        var crater = VolcanBuilder.BuildSummit();
        var cima = new GameObject("Cima");
        foreach (var g in UnityEngine.SceneManagement.SceneManager.GetActiveScene().GetRootGameObjects())
            if (!before.Contains(g) && g != cima) g.transform.SetParent(cima.transform, true);
        cima.transform.position = new Vector3(sx, H5 - 300f, 0f);

        var arena = Object.FindFirstObjectByType<SummitArena>();
        arena.boss.floorY = H5;
        var ab = arena.gameObject.AddComponent<BoxCollider>();
        ab.isTrigger = true; ab.center = new Vector3(0, 312f, 0); ab.size = new Vector3(46f, 24f, 46f);
        Kinematic(arena.gameObject);
        Prim(PrimitiveType.Cylinder, "BaseCima", new Vector3(sx, H5 * 0.5f - 1f, 0f), new Vector3(50f, H5 * 0.5f, 50f), rock);

        // flancos de la montana (paredes enormes a los lados) y zona de caida
        float x0 = 75f, x1 = sx + 40f;
        EvilKit.Skin(Prim(PrimitiveType.Cube, "MonteN", new Vector3((x0 + x1) * 0.5f, 45f, 40f), new Vector3(x1 - x0, 90f, 44f), rock), 6f);
        EvilKit.Skin(Prim(PrimitiveType.Cube, "MonteS", new Vector3((x0 + x1) * 0.5f, 45f, -40f), new Vector3(x1 - x0, 90f, 44f), rock), 6f);
        EvilKit.Flanks(x0, x1, 90f);
        var kz = Prim(PrimitiveType.Cube, "ZonaCaida", new Vector3((x0 + x1) * 0.5f, -25f, 0), new Vector3(x1 - x0 + 300f, 1f, 400f), Mat("Caida", Color.black));
        kz.GetComponent<BoxCollider>().isTrigger = true;
        kz.GetComponent<Renderer>().enabled = false;
        Kinematic(kz);
        kz.AddComponent<KillZone>();

        Zones = zones;
        return crater;
    }

    internal static MountainZone[] Zones;

    // rampa de xr a xr+26 subiendo de h0 a h1, con barandas
    static void Ramp(float xr, float h0, float h1, Material rock)
    {
        float ang = Mathf.Atan2(h1 - h0, RampLen), L = Mathf.Sqrt(RampLen * RampLen + (h1 - h0) * (h1 - h0)) + 1f;
        float sn = Mathf.Sin(ang), cs = Mathf.Cos(ang);
        Vector3 mid = new Vector3(xr + RampLen * 0.5f, (h0 + h1) * 0.5f, 0f);
        Quaternion rot = Quaternion.Euler(0, 0, ang * Mathf.Rad2Deg);
        Vector3 n = new Vector3(-sn, cs, 0f);
        var body = Prim(PrimitiveType.Cube, "Rampa", mid - n * 0.5f, new Vector3(L, 1f, RampW), Mat("MRampa", new Color(0.5f, 0.42f, 0.34f)));
        EvilKit.Skin(body, 3.5f, false, EvilKit.FloorMat("Rampa", new Color(0.5f, 0.42f, 0.34f), 30));
        body.transform.rotation = rot;
        foreach (float z in new[] { RampW * 0.5f + 0.3f, -RampW * 0.5f - 0.3f })
        {
            var rail = Prim(PrimitiveType.Cube, "Baranda", mid + n * 1.25f + new Vector3(0, 0, z), new Vector3(L, 2.5f, 0.6f), rock);
            EvilKit.Skin(rail, 3f);
            rail.transform.rotation = rot;
        }
        // balcones sobresaliendo de la muralla de la subida (decorativos, con columnas hasta el suelo)
        for (int bi = 0; bi < 3; bi++)
        {
            float t = 0.2f + bi * 0.3f;
            float bx = xr + t * RampLen;
            float ys = h0 + t * (h1 - h0) + 2.3f;       // a la altura del borde de la baranda
            foreach (float sg in new[] { 1f, -1f })
            {
                float zs = sg * (RampW * 0.5f + 0.6f + 1.5f);
                EvilKit.Skin(Prim(PrimitiveType.Cube, "BalconLosa", new Vector3(bx, ys, zs), new Vector3(3.4f, 0.4f, 3f), rock), 2.5f);
                EvilKit.Skin(Prim(PrimitiveType.Cube, "BalconMensula", new Vector3(bx, ys - 0.7f, sg * (RampW * 0.5f + 0.6f + 0.6f)), new Vector3(2.4f, 1f, 1.2f), rock), 2f);
                foreach (float dx in new[] { -1.4f, 1.4f })
                    EvilKit.Skin(Prim(PrimitiveType.Cube, "BalconColumna", new Vector3(bx + dx, (ys - 0.2f) * 0.5f, sg * (RampW * 0.5f + 0.6f + 2.7f)), new Vector3(0.7f, ys - 0.2f, 0.7f), rock), 3f);
                // balaustrada
                float zo = sg * (RampW * 0.5f + 0.6f + 2.85f);
                Prim(PrimitiveType.Cube, "BalconBarandal", new Vector3(bx, ys + 1f, zo), new Vector3(3.4f, 0.18f, 0.2f), rock, null, false);
                foreach (float dx in new[] { -1.6f, -0.55f, 0.55f, 1.6f })
                    Prim(PrimitiveType.Cube, "BalconPoste", new Vector3(bx + dx, ys + 0.6f, zo), new Vector3(0.18f, 0.8f, 0.18f), rock, null, false);
                Prim(PrimitiveType.Cube, "BalconFarol", new Vector3(bx + 1.6f, ys + 1.25f, zo), new Vector3(0.28f, 0.34f, 0.28f), Mat("MFarolBalcon", new Color(1f, 0.25f, 0.1f), true), null, false);
            }
        }

        if (h0 > 0.1f)   // relleno bajo la rampa
            EvilKit.Skin(Prim(PrimitiveType.Cube, "RellenoRampa", new Vector3(xr + RampLen * 0.5f, h0 * 0.5f - 0.5f, 0f), new Vector3(RampLen, h0 + 1f, RampW), rock), 5f);
    }

    // decoracion de cada zona (sin colision)
    static void Decor(int k, float xt, float h, System.Random r)
    {
        if (DungeonKit.Available) DungeonKit.Decor(k, xt, h, r);   // primero lo solido (columnas, estatuas); la naturaleza se acomoda alrededor
        if (NatureKit.Available) { DecorKit(k, xt, h, r); return; }
        for (int i = 0; i < 9; i++)
        {
            float x = xt + 3f + (float)r.NextDouble() * (TerrLen - 6f);
            float z = (r.Next(2) == 0 ? 1 : -1) * (15.2f + (float)r.NextDouble() * 1f);
            float s = 1.5f + (float)r.NextDouble() * 2.5f;
            switch (k)
            {
                case 1:
                    Prim(PrimitiveType.Cylinder, "Tronco", new Vector3(x, h + s, z), new Vector3(0.6f, s, 0.6f), Mat("CTronco", new Color(0.35f, 0.24f, 0.14f)), null, false);
                    Prim(PrimitiveType.Sphere, "Copa", new Vector3(x, h + s * 2f + 1f, z), Vector3.one * (2.5f + s * 0.5f), Mat("CHoja", new Color(0.2f, 0.42f, 0.18f)), null, false);
                    break;
                case 2:
                    var root = Prim(PrimitiveType.Cylinder, "Raiz", new Vector3(x, h + 0.5f, z), new Vector3(0.5f, s * 1.5f, 0.5f), Mat("MRaiz", new Color(0.3f, 0.2f, 0.1f)), null, false);
                    root.transform.rotation = Quaternion.Euler(90f, (float)r.NextDouble() * 180f, 0f);
                    break;
                case 3:
                    var ice = Prim(PrimitiveType.Cube, "Hielo", new Vector3(x, h + s, z), new Vector3(1f, s * 2f, 1f), Mat("MHielo", new Color(0.6f, 0.85f, 1f), true), null, false);
                    ice.transform.rotation = Quaternion.Euler(0f, 45f, (float)(r.NextDouble() * 20 - 10));
                    break;
                default:
                    Prim(PrimitiveType.Cylinder, "ColumnaRota", new Vector3(x, h + s * 0.5f + 0.2f, z), new Vector3(1f, s * 0.5f + 0.5f, 1f), Mat("MColumna", new Color(0.55f, 0.55f, 0.58f)), null, false);
                    break;
            }
        }
    }

    // Naturaleza descuidada en los bordes de cada terraza (el centro queda libre para los puzzles)
    static void DecorKit(int k, float xt, float h, System.Random r)
    {
        var root = new GameObject("NaturalezaPiso" + k).transform;
        float x0 = xt + 1.5f, x1 = xt + TerrLen - 1.5f;
        System.Func<Vector3, bool> none = null;
        foreach (float sg in new[] { 1f, -1f })
        {
            float za = sg > 0 ? 15.4f : -16.6f, zb = sg > 0 ? 16.6f : -15.4f;
            switch (k)
            {
                case 1:   // bosque abandonado
                    NatureKit.Scatter(root, r, 3, x0, x1, za, zb, h, NatureKit.Trees, 0.9f, 1.2f, 0.6f, "trunk", none);
                    NatureKit.Scatter(root, r, 2, x0, x1, za, zb, h, NatureKit.Dead, 0.8f, 1.1f, 0.6f, "trunk", none);
                    NatureKit.Scatter(root, r, 4, x0, x1, za, zb, h, NatureKit.Bushes, 0.8f, 1.2f, 0.5f, null, none);
                    break;
                case 2:   // laberinto de raices: solo arboles muertos
                    NatureKit.Scatter(root, r, 6, x0, x1, za, zb, h, NatureKit.Dead, 0.8f, 1.2f, 0.6f, "trunk", none);
                    NatureKit.Scatter(root, r, 3, x0, x1, za, zb, h, NatureKit.Bushes, 0.7f, 1.0f, 0.5f, null, none);
                    break;
                case 3:   // paso helado: pinos y rocas
                    NatureKit.Scatter(root, r, 4, x0, x1, za, zb, h, NatureKit.Pines, 0.9f, 1.3f, 0.6f, "trunk", none);
                    NatureKit.Scatter(root, r, 4, x0, x1, za, zb, h, NatureKit.Rocks, 0.9f, 1.8f, 0.5f, "box", none);
                    break;
                default:  // ruinas: arboles muertos, rocas, armas olvidadas
                    NatureKit.Scatter(root, r, 3, x0, x1, za, zb, h, NatureKit.Dead, 0.8f, 1.1f, 0.6f, "trunk", none);
                    NatureKit.Scatter(root, r, 4, x0, x1, za, zb, h, NatureKit.Rocks, 0.9f, 1.8f, 0.5f, "box", none);
                    break;
            }
            NatureKit.Scatter(root, r, 40, x0, x1, za, zb, h, NatureKit.Grass, 1.0f, 2.0f, 0.2f, null, none);
            if (k != 3) NatureKit.Scatter(root, r, 6, x0, x1, za, zb, h, NatureKit.Flowers, 0.9f, 1.5f, 0.3f, null, none);
        }
        if (k == 4 && NatureKit.RpgAvailable)
        {
            NatureKit.Place(root, true, "Sword", new Vector3(xt + 12f, h + 0.5f, 15.8f), 20f, 0.38f);
            NatureKit.Place(root, true, "Shield", new Vector3(xt + 24f, h, -15.8f), 160f, 0.4f, "box");
            NatureKit.Place(root, true, "Barrel", new Vector3(xt + 30f, h, 15.9f), 0f, 0.36f, "box");
            NatureKit.Place(root, true, "KnightHelmet", new Vector3(xt + 6f, h, -15.9f), 100f, 0.3f);
        }
        MarkStatic(root);
    }

    // Con el kit medieval usa modelos 3D; sin el, las casas simples de siempre
    static void HouseAuto(float x, float z, float w, float d, Color col, bool doorToPositiveZ, string style)
    {
        if (MedievalKit.Available) MedievalKit.House(style, new Vector3(x, 0f, z), doorToPositiveZ ? 0f : 180f);
        else House(x, z, w, d, col, doorToPositiveZ);
    }

    static void House(float x, float z, float w, float d, Color col, bool doorToPositiveZ)
    {
        var wallMat = Mat("CCasa" + col.GetHashCode(), col);
        Prim(PrimitiveType.Cube, "Casa", new Vector3(x, 1.75f, z), new Vector3(w, 3.5f, d), wallMat);
        Prim(PrimitiveType.Cube, "Techo", new Vector3(x, 3.75f, z), new Vector3(w + 0.9f, 0.5f, d + 0.9f), Mat("CTecho", new Color(0.55f, 0.18f, 0.14f)));
        float side = doorToPositiveZ ? d * 0.5f + 0.03f : -(d * 0.5f + 0.03f);
        Prim(PrimitiveType.Cube, "Puerta", new Vector3(x, 1.1f, z + side), new Vector3(1.3f, 2.2f, 0.1f), Mat("CPuertaCasa", new Color(0.3f, 0.18f, 0.1f)), null, false);
        Prim(PrimitiveType.Cube, "Ventana", new Vector3(x + w * 0.3f, 2f, z + side), new Vector3(1f, 1f, 0.1f), Mat("CVentana", new Color(1f, 0.85f, 0.4f), true), null, false);
    }

    // dir: -1 = el camino queda hacia +z (vendedor atras, en -z); 1 = camino hacia -z
    // Tienda pegada a una casa: center = centro de la huella (6 x 5), yaw 0 = frente a +Z (casa del lado -X)
    static void Stall(string vendorName, string label, Vector3 center, float yaw, Color vendorColor)
    {
        var root = new GameObject("Tienda_" + vendorName);
        root.transform.position = center;
        root.transform.rotation = Quaternion.Euler(0f, yaw, 0f);

        var sense = root.AddComponent<SphereCollider>();   // zona sensible para tocar al vendedor
        sense.isTrigger = true; sense.center = new Vector3(0f, 1.2f, 0f); sense.radius = 2.8f;
        var v = root.AddComponent<Vendor>();
        v.vendorName = vendorName;
        v.interactDistance = 11f;

        try { FantasyKit.BuildShop(root.transform, vendorName, vendorColor); }
        catch (System.Exception ex) { Debug.LogError("Tienda " + vendorName + " (BuildShop): " + ex); }

        // vendedor: modelo 3D si existe; si no, capsula con ranura para tu modelo
        bool model = false;
        try
        {
        if (vendorName == "Mercader")
            model = RacesKit.Available ? RacesKit.Spawn(root.transform, "Vendedor_Mercader", "Orc_Male", new Vector3(0f, 0.1f, 0.5f), 0f, 2, "Idle_Loop", true) != null
                  : (UalKit.Available && UalKit.Spawn(root.transform, "Vendedor_Mercader", new Vector3(0f, 0.1f, -0.2f), 0f, vendorColor, "Idle_Loop", true) != null);
        else
            model = FantasyKit.PlaceVendorModel(root.transform, vendorName, new Vector3(0f, 0.1f, 0.5f), 0f, 1.9f);
        }
        catch (System.Exception ex) { Debug.LogError("Tienda " + vendorName + " (vendedor): " + ex); model = false; }
        if (!model)
        {
            var npc = Prim(PrimitiveType.Capsule, "Vendedor", new Vector3(0f, 1.1f, 0f), new Vector3(0.9f, 1f, 0.9f), Mat("CVend" + vendorName, vendorColor * 1.3f), root.transform, true);
            npc.AddComponent<ModelSlot>().slotName = "Vendedor_" + vendorName;
        }

        Label(label, center + new Vector3(0, 5.4f, 0)).AddComponent<ProximityLabel>().radius = 15f;
    }
}
