using TMPro;
using UnityEngine;
using static ZonaPruebasBuilder;

// Galeria de objetos: un salon con una vitrina por cada objeto del juego, con su cartel explicativo.
// Camina a lo largo (A/D o joystick) y lee. Los objetos de puzzle (cubo, placa, baliza, respiradero) funcionan de verdad.
internal static class GaleriaBuilder
{
    const float Spacing = 6f;

    internal static LavaCrater Build(out Vector3 start)
    {
        start = new Vector3(-8f, 0f, 0f);   // mirando hacia +X, hacia las vitrinas

        RenderSettings.fog = false;
        RenderSettings.ambientMode = UnityEngine.Rendering.AmbientMode.Flat;
        RenderSettings.ambientLight = new Color(0.75f, 0.75f, 0.8f);

        // ---------- Salon ----------
        var wall = Mat("GWall", new Color(0.22f, 0.22f, 0.28f));
        Prim(PrimitiveType.Cube, "Suelo", new Vector3(-4, -0.5f, 0), new Vector3(24, 1, 160), Mat("GSuelo", new Color(0.3f, 0.3f, 0.33f)));
        Prim(PrimitiveType.Cube, "Muro_Fondo", new Vector3(8, 5, 0), new Vector3(0.5f, 10, 160), wall);
        Prim(PrimitiveType.Cube, "Muro_Atras", new Vector3(-16, 5, 0), new Vector3(0.5f, 10, 160), wall);
        Prim(PrimitiveType.Cube, "Muro_Norte", new Vector3(-4, 5, 80), new Vector3(24, 10, 0.5f), wall);
        Prim(PrimitiveType.Cube, "Muro_Sur", new Vector3(-4, 5, -80), new Vector3(24, 10, 0.5f), wall);

        GalLabel("GALERIA DE OBJETOS", new Vector3(-6f, 9f, 0f), 12f, new Vector2(24, 3), Color.white);
        GalLabel("Camina a lo largo del salon (A / D) y lee cada cartel.\nCubo, placa, baliza y respiradero funcionan de verdad.\nPalanca, puerta, cofre, maquina, cristal e interruptor estan en 'Probar controles'.",
                 new Vector3(-6f, 6.8f, 0f), 3.2f, new Vector2(24, 4), new Color(1f, 0.9f, 0.6f));

        // ---------- Vitrinas ----------
        int n = 0;

        // 1-4 Personajes (modelo 3D + diseno; si no hay modelos queda la capsula)
        {
            Vector3 p = Slot(n++);
            Pedestal(p);
            if (!Pj(p, "Hobbit", "Elf_Female", 3, 0.8f, Role.Hobbit))
                Prim(PrimitiveType.Capsule, "Hobbit", p + new Vector3(0, 1.5f, 0), new Vector3(0.8f, 0.8f, 0.8f), Mat("Pj_Hobbit", new Color(0.4f, 0.7f, 0.3f)), null, false);
            Card("HOBBIT", "Pequeño, agil y muy resistente a la carga del artefacto. Lleva gorro con pluma, mochila y bufanda.\nHabilidad: agacharse (boton Habilidad, Y en VR) para pasar por huecos bajos.", p);
        }
        {
            Vector3 p = Slot(n++);
            Pedestal(p);
            if (!Pj(p, "Mago", "Elf_Male", 2, 1f, Role.Mage))
                Prim(PrimitiveType.Capsule, "Mago", p + new Vector3(0, 1.8f, 0), Vector3.one, Mat("Pj_Mago", new Color(0.5f, 0.3f, 0.8f)), null, false);
            Card("MAGO", "Sombrero con estrella, capa y baston con orbe. Dispara magia hacia la mira: rompe rocas y activa cristales.\nVR: ambos gatillos forman un arco; mas separacion de manos = mas carga.", p);
        }
        {
            Vector3 p = Slot(n++);
            Pedestal(p);
            if (!Pj(p, "Ingeniero", "Orc_Female", 2, 1f, Role.Engineer))
                Prim(PrimitiveType.Capsule, "Ingeniero", p + new Vector3(0, 1.8f, 0), Vector3.one, Mat("Pj_Ingeniero", new Color(0.9f, 0.55f, 0.15f)), null, false);
            Card("INGENIERO", "Antiparras, mochila con tanques y cinturon de herramientas. Gancho libre: apunta a cualquier superficie (hasta 28 m) y sales disparado.", p);
        }
        {
            Vector3 p = Slot(n++);
            Pedestal(p);
            if (!Pj(p, "Peleador", "Orc_Male", 3, 1.1f, Role.Fighter))
                Prim(PrimitiveType.Capsule, "Peleador", p + new Vector3(0, 1.8f, 0), Vector3.one, Mat("Pj_Peleador", new Color(0.75f, 0.15f, 0.15f)), null, false);
            Card("PELEADOR (SOLO VR)", "Casco con cresta, hombreras y capa. Se juega solo con casco VR: espada, hacha, maza y escudo en cada mano. Pega a los enemigos moviendo el brazo.", p);
        }
        // 4 Artefacto
        {
            Vector3 p = Slot(n++);
            Pedestal(p);
            var a = Prim(PrimitiveType.Sphere, "Ascua", p + new Vector3(0, 1.6f, 0), Vector3.one * 0.7f, Mat("Artefacto", new Color(1f, 0.85f, 0.2f), true), null, false);
            a.AddComponent<ArtifactVisual>();   // reloj de arena cosmico (se arma al ejecutar)
            var lg = new GameObject("Luz"); lg.transform.position = a.transform.position;
            var l = lg.AddComponent<Light>(); l.type = LightType.Point; l.range = 8f; l.intensity = 5f; l.color = new Color(1f, 0.7f, 0.2f);
            Card("EL ASCUA (RELOJ DE ARENA)", "Reloj de arena cosmico que siempre llevas. Si lo sueltas mas de 5 minutos lo pierdes.\nEn VR va en tu mano izquierda (ocupada: sin arco ni gancho con esa mano); el hobbit lo guarda en su mochila y conserva las dos manos.", p);
        }
        // 5 Carga del artefacto
        {
            Vector3 p = Slot(n++);
            Pedestal(p);
            float[] hs = { 0.9f, 0.5f, 0.2f };
            Color[] cs = { new Color(0.4f, 0.7f, 0.3f), new Color(0.5f, 0.3f, 0.8f), new Color(0.9f, 0.55f, 0.15f) };
            for (int i = 0; i < 3; i++)
                Prim(PrimitiveType.Cube, "Barra" + i, p + new Vector3(0, 0.8f + hs[i] * 0.5f, (i - 1) * 0.7f), new Vector3(0.5f, hs[i], 0.5f), Mat("Barra" + i, cs[i]), null, false);
            Card("CARGA DEL ARTEFACTO", "Quien lo lleva se corrompe. El hobbit lo resiste mejor.\n70%: vas lento. 100%: te vence y hay relevo forzado. Los que descansan se recuperan; el relevo da impulso.", p);
        }
        // 6 Cubo
        PuzzleCube cube;
        {
            Vector3 p = Slot(n++);
            Pedestal(p);
            cube = VolcanBuilder.MakeCube("Cubo_Galeria", p + new Vector3(0, 0.8f, 0));
            Card("CUBO DE PUZZLE", "Tocalo para tomarlo y toca de nuevo para lanzarlo. VR: grip para agarrar y soltar lanzando.\nSi cae al vacio vuelve a su lugar. Llevalo a la placa de la siguiente vitrina.", p);
        }
        // 7 Placa + 8 Baliza + (pad en la 9)
        CheckpointBeacon beacon;
        BoostPad pad;
        Vector3 pPlate = Slot(n++);
        Vector3 pBeacon = Slot(n++);
        Vector3 pPad = Slot(n++);
        beacon = VolcanBuilder.MakeBeacon("Baliza_Galeria", pBeacon + new Vector3(0, 0.8f, 0));
        Pedestal(pBeacon);
        pad = VolcanBuilder.MakePad("Respiradero_Galeria", pPad);
        pad.horizontalDirection = Vector3.right;
        Pedestal(pPlate);
        VolcanBuilder.MakePlate("Placa_Galeria", pPlate + new Vector3(0, 0.8f, 0), beacon, pad);
        Card("PLACA DE PRESION", "Solo responde a cubos (tu no la activas). Mientras haya un cubo encima, activa lo que tenga conectado: aqui la baliza y el respiradero.\nSe pueden combinar varias con una compuerta Y.", pPlate);
        Card("BALIZA DE CHECKPOINT", "Recompensa de puzzle: al encenderse guarda tu punto de reaparicion mas arriba en la montaña.", pBeacon);
        Card("RESPIRADERO DE LAVA", "Se enciende al resolver un puzzle. Al pisarlo sales disparado hacia arriba: el salto amplio para subir mas rapido.", pPad, false);

        // 10 Roca
        {
            Vector3 p = Slot(n++);
            Pedestal(p);
            var r = Prim(PrimitiveType.Sphere, "BolaFuego", p + new Vector3(0, 1.4f, 0), Vector3.one * 1.1f, Mat("BolaFuegoMat", new Color(1f, 0.42f, 0.05f), true), null, false);
            r.AddComponent<ModelSlot>().slotName = "BolaFuego";
            var sh = Prim(PrimitiveType.Cylinder, "SombraRoca", p + new Vector3(-1.6f, 0.03f, 0), new Vector3(1.8f, 0.01f, 1.8f), Mat("Sombra", new Color(0.9f, 0.15f, 0.1f)), null, false);
            Card("BOLA DE FUEGO", "Cae en oleadas: calma, aviso y rafaga. La sombra roja del suelo marca donde cae.\nExplota en un area chica: te quita una vida y te empuja suave. La magia la destruye (a veces suelta una moneda).", p);
        }
        // 11 Agarres de escalada (VR)
        {
            Vector3 p = Slot(n++);
            VolcanBuilder.Block("Muro_Escalada", p + new Vector3(0.65f, 3f, 0), new Vector3(2f, 6f, 4f), "VPlataforma");
            var mat = Mat("Agarre", new Color(0.9f, 0.8f, 0.25f), true);
            for (int k = 0; k < 7; k++)
            {
                float y = 0.8f + k * 0.8f;
                var h = Prim(PrimitiveType.Cube, k == 6 ? "Agarre_borde" : "Agarre", p + new Vector3(-0.5f, y, (k % 2 == 0 ? -0.25f : 0.25f)), new Vector3(0.18f, 0.35f, 0.35f), mat);
                h.GetComponent<BoxCollider>().isTrigger = true;
                var hold = h.AddComponent<ClimbHold>();
                if (k == 6) { hold.mantle = true; hold.mantleOffset = new Vector3(1.2f, 1.4f, 0f); }
            }
            Card("AGARRES DE ESCALADA (VR)", "Con el grip te cuelgas de los agarres amarillos y subes mano sobre mano.\nAl llegar al borde te sube solo. Si sueltas en el aire sales con impulso.", p + new Vector3(0, 1.0f, 0));
        }
        // 12 Crater
        LavaCrater crater;
        {
            Vector3 p = Slot(n++);
            var lava = Cylinder("Lava", p + new Vector3(0, 0.05f, 0), 2.2f, 0.1f, Mat("VLava", new Color(1f, 0.35f, 0.05f), true));
            crater = lava.AddComponent<LavaCrater>();
            crater.prompt = "Crater";
            crater.interactDistance = 10f;
            var lg = new GameObject("LuzLava"); lg.transform.position = p + new Vector3(0, 2f, 0);
            var l = lg.AddComponent<Light>(); l.type = LightType.Point; l.range = 12f; l.intensity = 8f; l.color = new Color(1f, 0.45f, 0.1f);
            Card("ESCOTILLA DEL CRATER", "Meta del juego: tocala llevando el Ascua; la tapa se abre y arrojas el artefacto.\n(Aqui es una demostracion: si lo haces, termina la partida de esta sala.)", p, false);
        }

        // 13 Moneda
        {
            Vector3 p = Slot(n++);
            Pedestal(p);
            var c = Prim(PrimitiveType.Cylinder, "Moneda", p + new Vector3(0, 1.4f, 0), new Vector3(0.9f, 0.08f, 0.9f), Mat("MonedaMat", new Color(1f, 0.82f, 0.15f), true), null, false);
            c.transform.rotation = Quaternion.Euler(90f, 0f, 0f);
            Card("MONEDA", "Se recoge al pasar cerca. Las encuentras en cada piso, en los cofres y al destruir bolas de fuego.\nAl caer o ganar se guardan en el banco para comprar mejoras en la Comarca.", p);
        }
        // 14 Cofre de monedas (funciona)
        {
            Vector3 p = Slot(n++);
            FB.MakeChest(null, p + new Vector3(0, 0f, 0), 8);
            Card("COFRE DE MONEDAS", "Tocalo para abrirlo: suelta monedas. Aparece en los pisos 1 y 3.", p);
        }
        // 15 Altar de reliquias (funciona)
        {
            Vector3 p = Slot(n++);
            FB.MakeAltar(null, p);
            Card("ALTAR DE RELIQUIAS", "Tocalo y elige 1 de 3 reliquias para esta partida: mas vida, velocidad, gancho, magia...\nAparece en los pisos 2 y 4. Se pierden al caer.", p);
        }
        // 16 Portal de salida
        {
            Vector3 p = Slot(n++);
            FB.MakeExit(null, p);
            Card("PORTAL DE SALIDA", "Cada piso tiene uno. Se abre (verde) cuando resuelves todos los puzzles del piso. Cruzarlo lleva al siguiente.", p);
        }
        // 17 Vendedor (funciona)
        {
            Vector3 p = Slot(n++);
            var root = new GameObject("VendedorGaleria");
            root.transform.position = p;
            Prim(PrimitiveType.Cube, "Mesa", new Vector3(0, 0.5f, 0), new Vector3(2.6f, 1f, 1.2f), Mat("CMadera", new Color(0.5f, 0.35f, 0.2f)), root.transform, false);
            Prim(PrimitiveType.Capsule, "Vendedor", new Vector3(0, 1f, 1.1f), new Vector3(0.9f, 1f, 0.9f), Mat("CVendHerrero", new Color(0.7f, 0.33f, 0.26f)), root.transform, false);
            var bc = root.AddComponent<BoxCollider>();
            bc.center = new Vector3(0, 1f, 0.4f); bc.size = new Vector3(3f, 2f, 3f);
            var v = root.AddComponent<Vendor>();
            v.vendorName = "Herrero";
            v.interactDistance = 8f;
            Card("VENDEDORES", "En la Comarca: Herrero (vida, relevo), Alquimista (menos carga, descanso) y Mercader (velocidad, gancho).\nTocalo para ver una tienda de prueba.", p);
        }
        // El Cronarca (jefe) con su aspecto real
        {
            Vector3 p = Slot(n++);
            Pedestal(p);
            var g = new GameObject("CronarcaGaleria");
            g.transform.position = p + new Vector3(0f, 4.2f, 0f);
            var gp = g.AddComponent<GalleryProp>();
            gp.mode = GalleryProp.Mode.Boss; gp.scale = 0.55f; gp.spin = 20f;
            Card("EL CRONARCA (JEFE)", "Reloj gigante, senor del tiempo, en la cima. Manecillas giratorias a ras de suelo (saltalas), bombas de tiempo y rebobinar: te devuelve 4 s atras.\nSe hiere con magia o lanzandole cubos.", p);
        }
        // Enemigos
        EnemySlot(ref n, "ZOMBI", "Enemigo comun de los pisos. Camina hacia ti y golpea; no atraviesa paredes. 3 de vida.", "Enemigos/Zombi", 1f);
        EnemySlot(ref n, "ZOMBI BRUTO", "Version grande y resistente (7 de vida). Aparece en los pisos altos.", "Enemigos/ZombiBruto", 1f);
        EnemySlot(ref n, "ORCO", "Guerrero con espada. Tambien puebla los pisos; el Peleador y la magia lo derrotan.", "Enemigos/Orco", 1f);
        // Armas
        {
            Vector3 p = Slot(n++);
            Pedestal(p);
            var g = new GameObject("ArmasGaleria");
            g.transform.position = p;
            g.AddComponent<GalleryProp>().mode = GalleryProp.Mode.Weapons;
            Card("ARMAS DEL PELEADOR", "Espada, hacha, maza y escudo. En VR se agarran con el grip y hacen dano segun la velocidad de la punta. El escudo bloquea golpes.", p);
        }

        return crater;
    }

    static bool Pj(Vector3 p, string name, string race, int variant, float scale, Role role)
    {
        if (!RacesKit.Available) return false;
        var h = RacesKit.Spawn(null, name, race, p + new Vector3(0f, 0.8f, 0f), 270f, variant, "Idle_Loop", false, scale);
        if (h == null) return false;
        var gp = h.AddComponent<GalleryProp>();
        gp.mode = GalleryProp.Mode.Character; gp.role = role;
        return true;
    }

    static void EnemySlot(ref int n, string title, string text, string res, float scale)
    {
        Vector3 p = Slot(n++);
        Pedestal(p);
        var g = new GameObject("Vitrina_" + title);
        g.transform.position = p + new Vector3(0f, 0.8f, 0f);
        g.transform.rotation = Quaternion.Euler(0f, 270f, 0f);
        var gp = g.AddComponent<GalleryProp>();
        gp.mode = GalleryProp.Mode.Prefab; gp.resource = res; gp.scale = scale; gp.spin = 0f;
        Card(title, text, p);
    }

    static Vector3 Slot(int i) => new Vector3(0f, 0f, (i - 11.5f) * Spacing);

    static void Pedestal(Vector3 p)
    {
        Prim(PrimitiveType.Cube, "Pedestal", p + new Vector3(0, 0.4f, 0), new Vector3(2.2f, 0.8f, 2.2f), Mat("GPedestal", new Color(0.4f, 0.4f, 0.45f)));
    }

    static void Card(string title, string body, Vector3 slot, bool unused = true)
    {
        GalLabel(title, slot + new Vector3(-0.8f, 5.6f, 0f), 6f, new Vector2(Spacing - 0.4f, 1.2f), new Color(1f, 0.8f, 0.3f));
        GalLabel(body, slot + new Vector3(-0.8f, 3.9f, 0f), 2.5f, new Vector2(Spacing - 0.4f, 2.8f), Color.white);
    }

    static void GalLabel(string text, Vector3 pos, float size, Vector2 box, Color color)
    {
        var go = new GameObject("Cartel");
        go.transform.position = pos;
        go.transform.rotation = Quaternion.Euler(0, 90, 0);   // legible mirando hacia +X
        var t = go.AddComponent<TextMeshPro>();
        t.text = text;
        t.fontSize = size;
        t.alignment = TextAlignmentOptions.Center;
        t.textWrappingMode = TextWrappingModes.Normal;
        t.color = color;
        t.rectTransform.sizeDelta = box;
    }
}
