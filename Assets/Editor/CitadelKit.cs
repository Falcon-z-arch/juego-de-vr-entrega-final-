using UnityEditor;
using UnityEngine;
using static ZonaPruebasBuilder;

// La Comarca como ciudadela blanca estilo Minas Tirith: terrazas escalonadas de piedra blanca a ambos lados,
// casas con tejados oscuros, almenas, torres, la Torre Blanca, muralla oeste y estandartes sobre el camino.
// Todo con primitivas + textura de ladrillo generada (Assets/Texturas/Muro_Blanco.png: puedes reemplazarla por la tuya).
internal static class CitadelKit
{
    internal static bool Enabled = true;
    static Material white;

    static Material White()
    {
        if (white != null) return white;
        EnsureFolder("Assets", "Texturas");
        string tp = "Assets/Texturas/Muro_Blanco.png";
        foreach (var n in new[] { "Blanco.png", "Blanco.jpg" })
            if (System.IO.File.Exists("Assets/Texturas/" + n)) { tp = "Assets/Texturas/" + n; break; }
        var tex = EvilKit.Bricks(tp, new Color(0.84f, 0.83f, 0.78f), 9);
        string path = "Assets/Materials/PiedraBlanca.mat";
        var m = AssetDatabase.LoadAssetAtPath<Material>(path);
        if (m == null)
        {
            m = new Material(Shader.Find("Universal Render Pipeline/Lit") ?? Shader.Find("Standard"));
            AssetDatabase.CreateAsset(m, path);
        }
        m.color = Color.white;
        if (m.HasProperty("_BaseColor")) m.SetColor("_BaseColor", Color.white);
        m.SetTexture("_BaseMap", tex);
        m.SetTexture("_MainTex", tex);
        m.SetFloat("_Smoothness", 0.08f);
        EditorUtility.SetDirty(m);
        white = m;
        return m;
    }

    static GameObject Block(Transform parent, string name, Vector3 pos, Vector3 size, float tile = 4f, bool collider = true)
    {
        var g = Prim(PrimitiveType.Cube, name, pos, size, White(), parent, collider);
        return EvilKit.Skin(g, tile, false, White());
    }

    static void Crenels(Transform parent, float x0, float x1, float y, float z, float step = 2f)
    {
        for (float x = x0; x <= x1; x += step)
            Block(parent, "Almena", new Vector3(x, y + 0.45f, z), new Vector3(1f, 0.9f, 0.7f), 2f, false);
    }

    internal static void Build()
    {
        if (!Enabled) return;
        var root = new GameObject("Ciudadela").transform;
        var rnd = new System.Random(5);
        var roofMat = Mat("CTejadoCiudad", new Color(0.2f, 0.22f, 0.3f));
        var bannerWhite = Mat("CEstandarteBlanco", new Color(0.92f, 0.93f, 0.97f));
        var bannerBlue = Mat("CEstandarteAzul", new Color(0.18f, 0.3f, 0.62f));
        var gold = Mat("CDoradoCiudad", new Color(0.85f, 0.7f, 0.25f), true);

        // ---------- terrazas escalonadas (norte y sur), cada una 4 m mas alta que la anterior ----------
        const int Tiers = 4;
        float z0 = 22.5f, depth = 5f, stepH = 4f;
        foreach (float sg in new[] { 1f, -1f })
        {
            for (int k = 0; k < Tiers; k++)
            {
                float za = z0 + k * depth, zb = 43f;                   // cada piso llega hasta el fondo
                float h = stepH * (k + 1);
                float zc = (za + zb) * 0.5f * sg;
                Block(root, "Terraza_" + k, new Vector3(0f, h * 0.5f - 0.5f, zc), new Vector3(146f, h + 1f, zb - za), 5f);
                // almenas del borde frontal
                Crenels(root, -70f, 70f, h, sg * (za + 0.4f));

                // casas sobre la terraza (con hueco para la torre)
                float x = -66f + (float)rnd.NextDouble() * 4f;
                while (x < 66f)
                {
                    float w = 5f + (float)rnd.NextDouble() * 4f, d = 3.2f, ht = 2.6f + (float)rnd.NextDouble() * 1.4f;
                    bool tower = sg > 0 && k == Tiers - 1 && Mathf.Abs(x) < 7f;
                    bool gate = Mathf.Abs(x - 62f) < 6f && false;
                    if (!tower && !gate)
                    {
                        float zz = sg * (za + 2.6f + (float)rnd.NextDouble() * 0.6f);
                        Block(root, "CasaCiudad", new Vector3(x, h + ht * 0.5f, zz), new Vector3(w, ht, d), 3f);
                        var roof = Prim(PrimitiveType.Cube, "Tejado", new Vector3(x, h + ht + 0.25f, zz), new Vector3(w + 0.5f, 0.5f, d + 0.5f), roofMat, root, false);
                        // ventanas oscuras al frente
                        int nwin = Mathf.Max(1, (int)(w / 2.2f));
                        for (int i = 0; i < nwin; i++)
                        {
                            float wx = x - w * 0.5f + (i + 0.5f) * (w / nwin);
                            Prim(PrimitiveType.Cube, "Ventana", new Vector3(wx, h + ht * 0.55f, zz - sg * (d * 0.5f + 0.02f)), new Vector3(0.7f, 1f, 0.05f), Mat("CVentanaCiudad", new Color(0.12f, 0.14f, 0.2f)), root, false);
                        }
                    }
                    x += w + 1.2f + (float)rnd.NextDouble() * 3f;
                }
            }
        }

        // ---------- Torre Blanca (norte, sobre la ultima terraza) ----------
        float baseY = stepH * Tiers;
        float tz = z0 + (Tiers - 1) * depth + 3.5f;
        Block(root, "TorreBlanca_base", new Vector3(0f, baseY + 9f, tz), new Vector3(9f, 18f, 9f), 4f);
        Block(root, "TorreBlanca_medio", new Vector3(0f, baseY + 22f, tz), new Vector3(6.4f, 8f, 6.4f), 4f);
        Block(root, "TorreBlanca_alto", new Vector3(0f, baseY + 31f, tz), new Vector3(4.2f, 10f, 4.2f), 4f);
        Prim(PrimitiveType.Cube, "TorreBlanca_punta", new Vector3(0f, baseY + 38.5f, tz), new Vector3(2.4f, 5f, 2.4f), gold, root, false);
        Prim(PrimitiveType.Cube, "TorreBlanca_mastil", new Vector3(0f, baseY + 43f, tz), new Vector3(0.18f, 5f, 0.18f), gold, root, false);
        Prim(PrimitiveType.Cube, "TorreBlanca_bandera", new Vector3(0f, baseY + 44.2f, tz - 1.1f), new Vector3(0.05f, 1.6f, 2.2f), bannerWhite, root, false);
        for (int i = 0; i < 6; i++)   // ventanas de la torre
            Prim(PrimitiveType.Cube, "TorreVentana", new Vector3((i % 2 == 0 ? -1.4f : 1.4f), baseY + 6f + i * 3.2f, tz - 4.52f), new Vector3(0.8f, 1.5f, 0.05f), Mat("CVentanaCiudad", new Color(0.12f, 0.14f, 0.2f)), root, false);
        var tl = new GameObject("LuzTorre").transform;
        tl.SetParent(root, false);
        tl.position = new Vector3(0f, baseY + 40f, tz - 3f);
        var li = tl.gameObject.AddComponent<Light>();
        li.type = LightType.Point; li.range = 40f; li.intensity = 6f; li.color = new Color(1f, 0.92f, 0.75f);

        // torre de guardia (sur)
        float sz = -(z0 + (Tiers - 1) * depth + 3.5f);
        Block(root, "TorreGuardia_base", new Vector3(-34f, baseY + 6f, sz), new Vector3(7f, 12f, 7f), 4f);
        Block(root, "TorreGuardia_alto", new Vector3(-34f, baseY + 15f, sz), new Vector3(5f, 6f, 5f), 4f);
        Prim(PrimitiveType.Cube, "TorreGuardia_techo", new Vector3(-34f, baseY + 18.5f, sz), new Vector3(5.8f, 0.8f, 5.8f), roofMat, root, false);
        Prim(PrimitiveType.Cube, "TorreGuardia_bandera", new Vector3(-34f, baseY + 20.5f, sz + 0.9f), new Vector3(0.05f, 1.4f, 1.8f), bannerBlue, root, false);

        // ---------- muralla oeste (detras del punto de inicio) con dos torres redondas ----------
        Block(root, "MurallaOeste", new Vector3(-72f, 5f, 0f), new Vector3(2.5f, 10f, 44f), 4f);
        for (float zc = -21f; zc <= 21f; zc += 2f) Block(root, "AlmenaO", new Vector3(-72f, 10.45f, zc), new Vector3(0.7f, 0.9f, 1f), 2f, false);
        foreach (float tzc in new[] { -22f, 22f })
        {
            var t = Prim(PrimitiveType.Cylinder, "TorreMuralla", new Vector3(-72f, 7f, tzc), new Vector3(7f, 7f, 7f), White(), root, true);
            Prim(PrimitiveType.Cylinder, "TorreMuralla_techo", new Vector3(-72f, 14.4f, tzc), new Vector3(7.8f, 0.4f, 7.8f), roofMat, root, false);
            Prim(PrimitiveType.Cube, "TorreMuralla_bandera", new Vector3(-72f, 16.6f, tzc + 1f), new Vector3(0.05f, 1.6f, 2f), bannerBlue, root, false);
        }

        // ---------- estandartes a lo largo del camino ----------
        for (float x = -42f; x <= 56f; x += 14f)
            foreach (float sg in new[] { 1f, -1f })
            {
                Prim(PrimitiveType.Cylinder, "AstaEstandarte", new Vector3(x, 2.6f, sg * 5.2f), new Vector3(0.15f, 2.6f, 0.15f), Mat("CAsta", new Color(0.35f, 0.25f, 0.15f)), root, false);
                Prim(PrimitiveType.Cube, "Estandarte", new Vector3(x + 0.05f, 3.6f, sg * 5.2f), new Vector3(0.05f, 2.2f, 1.1f), ((int)(x / 14f) % 2 == 0) ? bannerWhite : bannerBlue, root, false);
                Prim(PrimitiveType.Sphere, "AstaPunta", new Vector3(x, 5.3f, sg * 5.2f), Vector3.one * 0.3f, gold, root, false);
            }

        // ---------- muralla fronteriza de Karnak: la puerta es la barrera entre reinos ----------
        foreach (float sg in new[] { 1f, -1f })
        {
            Block(root, "MurallaKarnak", new Vector3(62f, 6f, sg * 24.2f), new Vector3(3f, 12f, 37.8f), 4f);
            for (float zc = 6.5f; zc <= 42f; zc += 2f) Block(root, "AlmenaK", new Vector3(62f, 12.45f, sg * zc), new Vector3(0.9f, 0.9f, 1f), 2f, false);
            Block(root, "TorreKarnak", new Vector3(62f, 9f, sg * 8.6f), new Vector3(5f, 18f, 5f), 4f);
            Prim(PrimitiveType.Cube, "TorreKarnak_techo", new Vector3(62f, 18.4f, sg * 8.6f), new Vector3(5.8f, 0.8f, 5.8f), roofMat, root, false);
            Prim(PrimitiveType.Cube, "TorreKarnak_bandera", new Vector3(62f, 20.6f, sg * 8.6f - 0.9f), new Vector3(0.05f, 1.8f, 1.8f), bannerBlue, root, false);
        }

        CitadelRoot = root;
        ComarcaBuilder.MarkStatic(root);
    }

    internal static Transform CitadelRoot;
}
