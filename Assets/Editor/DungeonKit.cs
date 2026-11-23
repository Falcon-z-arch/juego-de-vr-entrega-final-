using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEngine;
using static ZonaPruebasBuilder;

// Packs "Modular Dungeon" (Quaternius, CC0). A = "Updated Modular Dungeon - May 2019", B = "Modular Dungeon Pack - Jan 2018".
// Carpetas esperadas: Assets/Modelos3D/DungeonA/FBX y Assets/Modelos3D/DungeonB/FBX.
// Cada piso de la montana recibe un tema distinto (Decor):
//   1 campamento abandonado | 2 catacumbas oscuras | 3 galeria de hielo | 4 fortaleza en ruinas
internal static class DungeonKit
{
    const string A = "Assets/Modelos3D/DungeonA/", B = "Assets/Modelos3D/DungeonB/";
    internal static bool Available => AssetDatabase.LoadAssetAtPath<GameObject>(A + "FBX/Wall_Modular.fbx") != null;

    static Color C(float r, float g, float b) => new Color(r, g, b);

    // colores (sRGB aproximado) tomados de los materiales de cada FBX
    static readonly Dictionary<string, Color> PA = new Dictionary<string, Color>
    {
        { "Bag", C(.48f,.38f,.30f) }, { "Bone", C(.80f,.72f,.60f) }, { "Ceramic", C(.48f,.42f,.34f) }, { "Cobweb", C(.95f,.95f,.95f) },
        { "DarkGrey_Floor", C(.24f,.24f,.28f) }, { "Grey_Floor", C(.31f,.30f,.35f) }, { "DarkMetal", C(.23f,.22f,.28f) },
        { "DarkSteel", C(.55f,.55f,.51f) }, { "DarkWood", C(.38f,.28f,.26f) }, { "LightSteel", C(.70f,.72f,.76f) },
        { "LightWood", C(.52f,.36f,.26f) }, { "Main", C(.75f,.14f,.12f) }, { "Marble", C(.78f,.77f,.74f) },
        { "Metal", C(.31f,.29f,.37f) }, { "Statue", C(.62f,.62f,.60f) }, { "Steel", C(.64f,.66f,.69f) }, { "Thread", C(.24f,.25f,.48f) },
        { "Wall_Dark", C(.48f,.42f,.34f) }, { "Wall_Highlights", C(.56f,.50f,.43f) }, { "Wall_Medium", C(.46f,.39f,.31f) },
        { "Wood", C(.48f,.36f,.32f) }, { "Gold", C(.78f,.71f,.43f) }, { "Fire", C(.95f,.50f,.15f) },
    };
    static readonly Dictionary<string, Color> PB = new Dictionary<string, Color>
    {
        { "Black", C(.17f,.17f,.17f) }, { "Blue", C(.17f,.14f,.47f) }, { "Bones", C(.80f,.72f,.60f) }, { "Candle", C(.90f,.82f,.69f) },
        { "Cork", C(.40f,.22f,.13f) }, { "Cover", C(.73f,.22f,.28f) }, { "DarkSteel", C(.41f,.41f,.41f) }, { "Fire", C(.95f,.40f,.15f) },
        { "Gold", C(.90f,.65f,.30f) }, { "Ink", C(.42f,.36f,.25f) }, { "Liquid", C(.10f,.60f,.10f) }, { "Paper", C(.86f,.82f,.72f) },
        { "Red", C(.90f,.29f,.26f) }, { "Rock", C(.63f,.63f,.63f) }, { "RockLight", C(.82f,.82f,.82f) }, { "Steel", C(.67f,.67f,.67f) },
        { "Wood", C(.59f,.36f,.24f) },
    };

    [MenuItem("Certamen2/7 - Preparar dungeon (materiales)")]
    internal static void Prepare()
    {
        if (!Directory.Exists(A + "FBX") && !Directory.Exists(B + "FBX")) return;
        PrepareFolder(A, "Assets/Modelos3D/DungeonA", PA);
        PrepareFolder(B, "Assets/Modelos3D/DungeonB", PB);
        MakeProps();
    }

    // Prefabs en Resources/Props que usa FloorBuilder en ejecucion (cofre de monedas y altar de reliquias)
    static void MakeProps()
    {
        EnsureFolder("Assets", "Resources");
        EnsureFolder("Assets/Resources", "Props");
        Prop("Cofre", "A:Chest", 1.4f, 1);
        Prop("Altar", "A:Pedestal", 1.3f, 1);
    }

    static void Prop(string name, string key, float s, int theme)
    {
        var root = new GameObject(name);
        var g = P(root.transform, key, 0f, 0f, 0f, 0f, s, null, theme);
        if (g != null) PrefabUtility.SaveAsPrefabAsset(root, "Assets/Resources/Props/" + name + ".prefab");
        Object.DestroyImmediate(root);
    }

    static string BaseName(string n)
    {
        int i = n.IndexOf('.');
        return i > 0 ? n.Substring(0, i) : n;
    }

    static void PrepareFolder(string dir, string root, Dictionary<string, Color> pal)
    {
        if (!Directory.Exists(dir + "FBX")) return;
        EnsureFolder(root, "Materials");
        var cache = new Dictionary<string, Material>();
        foreach (var raw in Directory.GetFiles(dir + "FBX", "*.fbx"))
        {
            string p = raw.Replace('\\', '/');
            var imp = AssetImporter.GetAtPath(p) as ModelImporter;
            if (imp == null) continue;
            bool dirty = false;
            if (!imp.bakeAxisConversion) { imp.bakeAxisConversion = true; dirty = true; }
            foreach (var o in AssetDatabase.LoadAllAssetsAtPath(p))
            {
                if (!(o is Material m)) continue;
                string bn = BaseName(m.name);
                if (!cache.TryGetValue(bn, out var mat)) { mat = MakeMat(dir, bn, pal); cache[bn] = mat; }
                imp.AddRemap(new AssetImporter.SourceAssetIdentifier(typeof(Material), m.name), mat);
                dirty = true;
            }
            if (dirty) imp.SaveAndReimport();
        }
        AssetDatabase.SaveAssets();
        Debug.Log("Dungeon " + root + ": " + cache.Count + " materiales listos.");
    }

    static Shader Lit() => Shader.Find("Universal Render Pipeline/Lit") ?? Shader.Find("Standard");

    static void SetColor(Material m, Color c)
    {
        m.color = c;
        if (m.HasProperty("_BaseColor")) m.SetColor("_BaseColor", c);
    }

    static Material MakeMat(string dir, string name, Dictionary<string, Color> pal)
    {
        string path = dir + "Materials/D_" + name + ".mat";
        var m = AssetDatabase.LoadAssetAtPath<Material>(path);
        if (m == null)
        {
            m = new Material(Lit()) { name = "D_" + name };
            AssetDatabase.CreateAsset(m, path);
        }
        Color c = pal.TryGetValue(name, out var cc) ? cc : new Color(0.55f, 0.55f, 0.55f);
        SetColor(m, c);
        m.SetFloat("_Smoothness", 0.12f);
        bool metal = name.Contains("Steel") || name == "Gold" || name == "Metal" || name == "DarkMetal";
        m.SetFloat("_Metallic", metal ? 0.35f : 0f);
        if (name == "Cobweb") m.SetFloat("_Cull", 0f);
        if (name == "Fire")
        {
            m.EnableKeyword("_EMISSION");
            m.SetColor("_EmissionColor", c * 2f);
        }
        EditorUtility.SetDirty(m);
        return m;
    }

    // variante del material con tinte (pisos 2 y 3)
    static readonly Dictionary<string, Material> variants = new Dictionary<string, Material>();
    static Material Variant(Material src, int theme)
    {
        string sp = AssetDatabase.GetAssetPath(src);
        if (string.IsNullOrEmpty(sp)) return src;
        string vp = Path.Combine(Path.GetDirectoryName(sp), Path.GetFileNameWithoutExtension(sp) + "_T" + theme + ".mat").Replace('\\', '/');
        if (variants.TryGetValue(vp, out var cached) && cached != null) return cached;
        var m = AssetDatabase.LoadAssetAtPath<Material>(vp);
        if (m == null)
        {
            m = new Material(src);
            AssetDatabase.CreateAsset(m, vp);
        }
        bool fire = src.name.EndsWith("Fire");
        Color c = src.HasProperty("_BaseColor") ? src.GetColor("_BaseColor") : src.color;
        Color t = theme == 2 ? new Color(0.62f, 0.58f, 0.60f) : (theme == 5 ? new Color(0.40f, 0.34f, 0.40f) : new Color(0.62f, 0.82f, 1f));
        Color nc = fire ? (theme == 3 ? new Color(0.35f, 0.75f, 1f) : (theme == 5 ? new Color(1f, 0.12f, 0.06f) : c)) : new Color(c.r * t.r, c.g * t.g, c.b * t.b);
        SetColor(m, nc);
        if (fire) { m.EnableKeyword("_EMISSION"); m.SetColor("_EmissionColor", nc * 2f); }
        EditorUtility.SetDirty(m);
        variants[vp] = m;
        return m;
    }

    static void Tint(GameObject go, int theme)
    {
        if (theme != 2 && theme != 3 && theme != 5) return;
        foreach (var r in go.GetComponentsInChildren<Renderer>())
        {
            var arr = r.sharedMaterials;
            for (int i = 0; i < arr.Length; i++) if (arr[i] != null) arr[i] = Variant(arr[i], theme);
            r.sharedMaterials = arr;
        }
    }

    // ------------------------------------------------------------------ colocacion
    static GameObject Load(string key)
    {
        string dir = key[0] == 'A' ? A : B;
        return AssetDatabase.LoadAssetAtPath<GameObject>(dir + "FBX/" + key.Substring(2) + ".fbx");
    }

    static Bounds Measure(GameObject go)
    {
        var rs = go.GetComponentsInChildren<Renderer>();
        if (rs.Length == 0) return new Bounds(go.transform.position, Vector3.zero);
        Bounds b = rs[0].bounds;
        foreach (var r in rs) b.Encapsulate(r.bounds);
        return b;
    }

    // "A:Nombre" / "B:Nombre". Apoyado en el suelo (y), centrado en x,z. col = "box" para colision.
    internal static GameObject P(Transform parent, string key, float x, float y, float z, float yaw, float s, string col, int theme)
    {
        var prefab = Load(key);
        if (prefab == null) return null;
        var holder = new GameObject(key.Substring(2));
        holder.transform.SetParent(parent, false);
        holder.transform.position = new Vector3(x, y, z);
        holder.transform.localScale = Vector3.one * s;
        var go = (GameObject)PrefabUtility.InstantiatePrefab(prefab);
        go.transform.SetParent(holder.transform, false);
        go.transform.localPosition = Vector3.zero;
        go.transform.localRotation = prefab.transform.localRotation;
        Tint(go, theme);
        Bounds b = Measure(go);
        go.transform.position += new Vector3(x - b.center.x, y - b.min.y, z - b.center.z);
        if (col != null)
        {
            b = Measure(go);
            var bc = holder.AddComponent<BoxCollider>();
            bc.center = holder.transform.InverseTransformPoint(b.center);
            bc.size = new Vector3(b.size.x / s, b.size.y / s, b.size.z / s) * 0.9f;
        }
        holder.transform.rotation = Quaternion.Euler(0f, yaw, 0f);
        return holder;
    }

    // Pieza de pared (antorcha, estandarte): el pivote queda en 'pivot' y el cuerpo se gira hacia 'normal' (hacia la sala).
    internal static GameObject W(Transform parent, string key, Vector3 pivot, Vector3 normal, float s, int theme)
    {
        var prefab = Load(key);
        if (prefab == null) return null;
        var holder = new GameObject(key.Substring(2));
        holder.transform.SetParent(parent, false);
        holder.transform.position = pivot;
        holder.transform.localScale = Vector3.one * s;
        var go = (GameObject)PrefabUtility.InstantiatePrefab(prefab);
        go.transform.SetParent(holder.transform, false);
        go.transform.localPosition = Vector3.zero;
        go.transform.localRotation = prefab.transform.localRotation;
        Tint(go, theme);
        Vector3 d = Measure(go).center - holder.transform.position;
        d.y = 0f;
        if (d.magnitude > 0.05f)
            holder.transform.rotation = Quaternion.Euler(0f, Vector3.SignedAngle(d, normal, Vector3.up), 0f);
        return holder;
    }

    // pila vertical (columna modular): devuelve la altura final
    static float Stack(Transform parent, string[] keys, float x, float y, float z, float s, int theme)
    {
        float cy = y;
        foreach (var k in keys)
        {
            var g = P(parent, k, x, cy, z, 0f, s, null, theme);
            if (g == null) continue;
            cy = Measure(g).max.y;
        }
        return cy;
    }

    static int Scatter(Transform parent, System.Random r, int count, float x0, float x1, float z0, float z1, float y,
                       string[] keys, float sMin, float sMax, float clear, string col, int theme)
    {
        count = Mathf.RoundToInt(count * ComarcaBuilder.TerrLen / 36f);
        int placed = 0;
        for (int i = 0; i < count; i++)
        {
            for (int tries = 0; tries < 12; tries++)
            {
                float x = Mathf.Lerp(x0, x1, (float)r.NextDouble()), z = Mathf.Lerp(z0, z1, (float)r.NextDouble());
                Physics.SyncTransforms();
                if (Physics.CheckSphere(new Vector3(x, y + clear + 0.15f, z), clear, ~0, QueryTriggerInteraction.Ignore)) continue;
                string k = keys[r.Next(keys.Length)];
                float s = Mathf.Lerp(sMin, sMax, (float)r.NextDouble());
                if (P(parent, k, x, y, z, (float)r.NextDouble() * 360f, s, col, theme) != null) placed++;
                break;
            }
        }
        return placed;
    }

    // muro de piedra a lo largo de los muros norte y sur (2 filas, 6 m de alto)
    static void Facade(Transform root, string key, float xt, float h, float yaw, float scale, int theme, System.Random r, float skipTop, float skipBottom)
    {
        float w = 2f * scale;      // ancho de cada panel
        int n = Mathf.CeilToInt(ComarcaBuilder.TerrLen / w);
        foreach (float sg in new[] { 1f, -1f })
            for (int i = 0; i < n; i++)
                for (int row = 0; row < 2; row++)
                {
                    float sk = row == 0 ? skipBottom : skipTop;
                    if (sk > 0f && r.NextDouble() < sk) continue;
                    P(root, key, xt + w * 0.5f + w * i, h + row * w, sg * (17f - 0.3f * scale), yaw, scale, null, theme);
                }
    }

    static GameObject TorchWall(Transform root, string key, float x, float h, float sg, float s, int theme)
        => W(root, key, new Vector3(x, h + 3.2f, sg * 16.6f), new Vector3(0f, 0f, -sg), s, theme);

    // ------------------------------------------------------------------ temas por piso
    internal static void Decor(int k, float xt, float h, System.Random r)
    {
        var root = new GameObject("DungeonPiso" + k).transform;
        switch (k)
        {
            case 1: Camp(root, xt, h, r); break;
            case 2: Catacombs(root, xt, h, r); break;
            case 3: Ice(root, xt, h, r); break;
            default: Fortress(root, xt, h, r); break;
        }
        ComarcaBuilder.MarkStatic(root);
    }

    // Piso 1: campamento abandonado en el bosque
    static void Camp(Transform root, float xt, float h, System.Random r)
    {
        const int T = 1;
        P(root, "A:Woodfire", xt + 8f, h, 15.7f, 0f, 1.5f, null, T);
        P(root, "A:Table_Big", xt + 13f, h, 15.9f, 0f, 1.1f, "box", T);
        P(root, "A:Chair", xt + 11.2f, h, 15.2f, 90f, 1.1f, null, T);
        P(root, "A:Chair", xt + 14.8f, h, 15.2f, 270f, 1.1f, null, T);
        P(root, "A:Barrel", xt + 18f, h, 16f, 20f, 1.2f, "box", T);
        P(root, "A:Barrel2", xt + 19.4f, h, 15.8f, 80f, 1.2f, "box", T);
        P(root, "A:Crate", xt + 21.5f, h, 16f, 15f, 1.2f, "box", T);
        P(root, "A:Bag_Standing", xt + 4.5f, h, 16f, 0f, 1.2f, null, T);
        P(root, "A:Bucket", xt + 9.8f, h, 16.1f, 0f, 1.3f, null, T);
        P(root, "A:Chest", xt + 30f, h, -15.9f, 180f, 1.3f, "box", T);
        P(root, "A:Barrel", xt + 27.8f, h, -16f, 0f, 1.2f, "box", T);
        P(root, "A:Crate", xt + 7f, h, -16f, 30f, 1.2f, "box", T);
        P(root, "B:Column_Broken", xt + 16f, h, -15.8f, 0f, 1.5f, "box", T);
        foreach (float sg in new[] { 1f, -1f })
        {
            float za = sg > 0 ? 15.2f : -16.4f, zb = sg > 0 ? 16.4f : -15.2f;
            Scatter(root, r, 2, xt + 1.5f, xt + ComarcaBuilder.TerrLen - 1.5f, za, zb, h, new[] { "B:Column_Broken", "B:Column_Broken2" }, 1.3f, 1.7f, 0.8f, "box", T);
            Scatter(root, r, 5, xt + 1.5f, xt + ComarcaBuilder.TerrLen - 1.5f, za, zb, h, new[] { "B:Rock1", "B:Rock2", "B:Rock3", "B:Rock4", "B:Rock5" }, 1.6f, 2.8f, 0.6f, "box", T);
        }
    }

    // Piso 2: catacumbas oscuras
    static void Catacombs(Transform root, float xt, float h, System.Random r)
    {
        const int T = 2;
        Facade(root, "A:Wall_Modular", xt, h, 0f, 1.5f, T, r, 0.35f, 0.12f);
        foreach (float sg in new[] { 1f, -1f })
        {
            for (int j = 0; j < 3; j++) TorchWall(root, "B:Torch_wall", xt + 6f + 12f * j, h, sg, 1.6f, T);
            float za = sg > 0 ? 15.2f : -16.4f, zb = sg > 0 ? 16.4f : -15.2f;
            Scatter(root, r, 4, xt + 1.5f, xt + ComarcaBuilder.TerrLen - 1.5f, za, zb, h, new[] { "B:Bones", "B:Bones2" }, 1.2f, 1.8f, 0.5f, null, T);
            Scatter(root, r, 4, xt + 1.5f, xt + ComarcaBuilder.TerrLen - 1.5f, za, zb, h, new[] { "A:Skull" }, 1.5f, 2.2f, 0.3f, null, T);
            Scatter(root, r, 2, xt + 1.5f, xt + ComarcaBuilder.TerrLen - 1.5f, za, zb, h, new[] { "B:Column_Broken", "B:Column_Broken2" }, 1.5f, 2.0f, 0.8f, "box", T);
            Scatter(root, r, 2, xt + 1.5f, xt + ComarcaBuilder.TerrLen - 1.5f, za, zb, h, new[] { "A:Barrel", "A:Barrel2", "A:Crate" }, 1.2f, 1.5f, 0.7f, "box", T);
            for (int j = 0; j < 4; j++)
                P(root, "A:Cobweb", xt + 2f + r.Next(0, 34), h + 4.6f + (float)r.NextDouble() * 1.2f, sg * 16.2f, sg > 0 ? 0f : 180f, 2.2f, null, T);
        }
        P(root, "B:Chest", xt + 24f, h, 16f, 200f, 1.4f, "box", T);
        P(root, "A:Chest", xt + 10f, h, -16f, 0f, 1.4f, "box", T);
    }

    // Piso 3: galeria de hielo
    static void Ice(Transform root, float xt, float h, System.Random r)
    {
        const int T = 3;
        Facade(root, "B:ModularStoneWall", xt, h, 90f, 1.5f, T, r, 0f, 0f);
        string[] col = { "B:ModularColumn_bottom", "B:ModularColumn_middle", "B:ModularColumn_middle", "B:ModularColumn_middle", "B:ModularColumn_middle", "B:ModularColumn_middle", "B:ModularColumn_top" };
        foreach (float sg in new[] { 1f, -1f })
        {
            for (int j = 0; j < 3; j++)
            {
                float cx = xt + 6f + 12f * j;
                Stack(root, col, cx, h, sg * 15.4f, 2.4f, T);
                var cc = new GameObject("ColisionColumna");
                cc.transform.SetParent(root, false);
                cc.transform.position = new Vector3(cx, h + 2f, sg * 15.4f);
                var cap = cc.AddComponent<CapsuleCollider>();
                cap.radius = 0.8f; cap.height = 4f;
                if (j != 1) P(root, "B:Candelabrum_tall", cx + 2.2f, h, sg * 15.8f, 0f, 1.4f, null, T);
            }
            float za = sg > 0 ? 15.2f : -16.4f, zb = sg > 0 ? 16.4f : -15.2f;
            Scatter(root, r, 5, xt + 1.5f, xt + ComarcaBuilder.TerrLen - 1.5f, za, zb, h, new[] { "B:Rock1", "B:Rock2", "B:Rock3", "B:Rock4", "B:Rock5" }, 2.4f, 4.0f, 0.8f, "box", T);
            Scatter(root, r, 2, xt + 1.5f, xt + ComarcaBuilder.TerrLen - 1.5f, za, zb, h, new[] { "B:Column_Broken", "B:Column_Broken2" }, 1.5f, 2.0f, 0.8f, "box", T);
        }
        // dos estatuas de caballo flanqueando la puerta del fondo
        foreach (float sg in new[] { 1f, -1f })
        {
            var ped = P(root, "A:Pedestal", xt + 32f, h, sg * 14.2f, 0f, 1.0f, "box", T);
            if (ped != null) P(root, "A:Statue_Horse", xt + 32f, Measure(ped).max.y, sg * 14.2f, sg > 0 ? 90f : 270f, 1.0f, null, T);
        }
    }

    // Piso 4: fortaleza en ruinas (calida, con estandartes)
    static void Fortress(Transform root, float xt, float h, System.Random r)
    {
        const int T = 4;
        Facade(root, "A:Wall_Modular", xt, h, 0f, 1.5f, T, r, 0.18f, 0f);
        foreach (float sg in new[] { 1f, -1f })
        {
            for (int j = 0; j < 4; j++)
            {
                float cx = xt + 3f + 10.5f * j;
                P(root, "A:Column", cx, h, sg * 15.3f, 0f, 1.2f, "box", T);
                P(root, "A:Torch", cx + 1.5f, h, sg * 15.9f, 0f, 1.6f, null, T);
            }
            foreach (float bx in new[] { 8f, 18f, 28f })
                W(root, "A:Banner_wall", new Vector3(xt + bx, h + 5.3f, sg * 16.6f), new Vector3(0f, 0f, -sg), 1.4f, T);
            float za = sg > 0 ? 15.2f : -16.4f, zb = sg > 0 ? 16.4f : -15.2f;
            Scatter(root, r, 2, xt + 1.5f, xt + ComarcaBuilder.TerrLen - 1.5f, za, zb, h, new[] { "A:Barrel", "A:Barrel2", "A:Crate" }, 1.2f, 1.5f, 0.7f, "box", T);
            Scatter(root, r, 3, xt + 1.5f, xt + ComarcaBuilder.TerrLen - 1.5f, za, zb, h, new[] { "B:Rock1", "B:Rock2", "B:Rock3" }, 1.4f, 2.4f, 0.6f, "box", T);
        }
        P(root, "A:Chest_Gold", xt + 20f, h, 15.9f, 180f, 1.4f, "box", T);
        P(root, "A:Coin_Pile", xt + 22.4f, h, 16f, 0f, 1.4f, null, T);
        P(root, "A:Table_Big", xt + 6f, h, -15.9f, 0f, 1.1f, "box", T);
        P(root, "A:Chair", xt + 4.2f, h, -15.3f, 90f, 1.1f, null, T);
        W(root, "A:Sword_WallMount", new Vector3(xt + 23f, h + 3.6f, -16.6f), new Vector3(0f, 0f, 1f), 1.3f, T);
        var ped = P(root, "A:Pedestal", xt + 31f, h, -14.4f, 0f, 1.0f, "box", T);
        if (ped != null) P(root, "A:Statue_Horse", xt + 31f, Measure(ped).max.y, -14.4f, 270f, 1.0f, null, T);
    }
}
