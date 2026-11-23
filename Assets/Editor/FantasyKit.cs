using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEngine;
using static ZonaPruebasBuilder;

// Kit "Fantasy Props MegaKit" (Quaternius, CC0): materiales URP + tiendas de los vendedores.
// Carpeta esperada: Assets/Modelos3D/FantasyProps/{FBX,Textures}
internal static class FantasyKit
{
    const string Dir = "Assets/Modelos3D/FantasyProps/";
    internal static bool Available => AssetDatabase.LoadAssetAtPath<GameObject>(Dir + "FBX/Stall_Empty.fbx") != null;

    [MenuItem("Certamen2/7 - Preparar kit de props fantasia (materiales)")]
    static void PrepareMenu()
    {
        if (!Directory.Exists(Dir + "FBX")) { EditorUtility.DisplayDialog("Props fantasia", "No encuentro " + Dir + "FBX", "OK"); return; }
        Prepare();
        EditorUtility.DisplayDialog("Props fantasia", "Materiales listos. Ahora ejecuta Certamen2 > 0 - Construir TODO.", "OK");
    }

    internal static void Prepare()
    {
        if (!Directory.Exists(Dir + "FBX")) return;
        AssetDatabase.Refresh();
        foreach (var f in Directory.GetFiles(Dir + "Textures", "*.png"))
        {
            var ti = AssetImporter.GetAtPath(f.Replace('\\', '/')) as TextureImporter;
            if (ti != null && ti.maxTextureSize != 1024) { ti.maxTextureSize = 1024; ti.SaveAndReimport(); }
        }
        EnsureFolder("Assets/Modelos3D/FantasyProps", "Materials");
        // texturas de los vendedores (la del herrero es de 4096 px): se bajan a 1024 para el celular
        foreach (var v in new[] { "Herrero", "Alquimista" })
        {
            string vd = "Assets/Modelos3D/Vendedores/" + v;
            if (!Directory.Exists(vd)) continue;
            foreach (var f in Directory.GetFiles(vd))
            {
                if (f.EndsWith(".meta") || !(f.EndsWith(".png") || f.EndsWith(".jpg"))) continue;
                var ti = AssetImporter.GetAtPath(f.Replace('\\', '/')) as TextureImporter;
                if (ti != null && ti.maxTextureSize != 1024) { ti.maxTextureSize = 1024; ti.SaveAndReimport(); }
            }
        }
        var cache = new Dictionary<string, Material>();
        foreach (var raw in Directory.GetFiles(Dir + "FBX", "*.fbx"))
        {
            string p = raw.Replace('\\', '/');
            var imp = AssetImporter.GetAtPath(p) as ModelImporter;
            if (imp == null) continue;
            bool dirty = false;
            if (!imp.bakeAxisConversion) { imp.bakeAxisConversion = true; dirty = true; }
            foreach (var o in AssetDatabase.LoadAllAssetsAtPath(p))
            {
                if (!(o is Material m)) continue;
                if (!cache.TryGetValue(m.name, out var mat)) { mat = GetMaterial(m.name); cache[m.name] = mat; }
                imp.AddRemap(new AssetImporter.SourceAssetIdentifier(typeof(Material), m.name), mat);
                dirty = true;
            }
            if (dirty) imp.SaveAndReimport();
        }
        AssetDatabase.SaveAssets();
        Debug.Log("Props fantasia: " + cache.Count + " materiales listos.");
    }

    static Material GetMaterial(string name)
    {
        string path = Dir + "Materials/" + name + ".mat";
        var existing = AssetDatabase.LoadAssetAtPath<Material>(path);
        if (existing != null) return existing;

        string n = name.ToLowerInvariant(), tex = null;
        Color tint = Color.white;
        float smooth = 0.2f, metal = 0f;
        if (n.Contains("metal")) { tex = "T_Trim_Metal_BaseColor"; metal = 0.5f; smooth = 0.45f; }
        else if (n.Contains("furniture")) tex = "T_Trim_Furniture_BaseColor";
        else if (n.Contains("cloth")) tex = "T_Trim_Cloth_BaseColor";
        else if (n.Contains("props")) tex = "T_Trim_Props_BaseColor";
        else if (n.Contains("banner")) { tex = "T_Trim_Cloth_BaseColor"; tint = new Color(0.8f, 0.2f, 0.15f); }
        else if (n.Contains("page")) tint = new Color(0.9f, 0.84f, 0.65f);
        else Debug.LogWarning("Props fantasia: material sin textura conocida: " + name);

        var mat = new Material(Shader.Find("Universal Render Pipeline/Lit") ?? Shader.Find("Standard")) { name = name };
        mat.SetFloat("_Smoothness", smooth);
        mat.SetFloat("_Metallic", metal);
        mat.color = tint;
        if (mat.HasProperty("_BaseColor")) mat.SetColor("_BaseColor", tint);
        if (tex != null)
        {
            var t = AssetDatabase.LoadAssetAtPath<Texture2D>(Dir + "Textures/" + tex + ".png");
            if (t != null) { mat.SetTexture("_BaseMap", t); mat.SetTexture("_MainTex", t); }
        }
        AssetDatabase.CreateAsset(mat, path);
        return mat;
    }

    // Pieza en coordenadas LOCALES de 'shop' (el frente de la tienda mira a +Z local).
    // tilt: inclinacion (grados, eje Z local) para espadas/herramientas apoyadas.
    static GameObject Put(Transform shop, string piece, Vector3 lp, float yaw, float scale, bool box = false, float tilt = 0f)
    {
        var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(Dir + "FBX/" + piece + ".fbx");
        if (prefab == null) { Debug.LogWarning("Props fantasia: falta " + piece); return null; }
        var holder = new GameObject(piece);
        holder.transform.SetParent(shop, false);
        holder.transform.localPosition = lp;
        holder.transform.localRotation = Quaternion.Euler(0f, yaw, tilt);
        holder.transform.localScale = Vector3.one * scale;
        var go = (GameObject)PrefabUtility.InstantiatePrefab(prefab);
        go.transform.SetParent(holder.transform, false);
        go.transform.localPosition = Vector3.zero;
        go.transform.localRotation = prefab.transform.localRotation;
        if (box)
        {
            var rends = go.GetComponentsInChildren<Renderer>();
            if (rends.Length > 0)
            {
                Bounds b = rends[0].bounds;
                foreach (var r in rends) b.Encapsulate(r.bounds);
                var bc = holder.AddComponent<BoxCollider>();
                bc.center = holder.transform.InverseTransformPoint(b.center);
                Vector3 ls = holder.transform.lossyScale;
                bc.size = new Vector3(b.size.x / Mathf.Max(ls.x, 0.001f), b.size.y / Mathf.Max(ls.y, 0.001f), b.size.z / Mathf.Max(ls.z, 0.001f)) * 0.9f;
            }
        }
        return holder;
    }

    // ---------------------------------------------------------------- tiendas pegadas a una casa
    static void Solid(Transform shop, string name, Vector3 lp, Vector3 size)
    {
        var g = GameObject.CreatePrimitive(PrimitiveType.Cube);
        g.name = name;
        g.transform.SetParent(shop, false);
        g.transform.localPosition = lp;
        g.transform.localScale = size;
        Object.DestroyImmediate(g.GetComponent<Renderer>());
        Object.DestroyImmediate(g.GetComponent<MeshFilter>());
    }

    // Local: el frente mira a +Z; la casa queda del lado -X. Huella 6 x 5 m.
    internal static void BuildShop(Transform shop, string vendor, Color accent)
    {
        bool med = MedievalKit.Available;
        var wood = Mat("CTiendaPiso", new Color(0.36f, 0.25f, 0.15f));
        var roofMat = Mat("CTiendaTecho", new Color(0.52f, 0.2f, 0.12f));
        var beam = Mat("CTiendaViga", new Color(0.25f, 0.17f, 0.1f));
        var cloth = Mat("CToldo" + vendor, accent);

        Prim(PrimitiveType.Cube, "Piso", new Vector3(0f, 0.05f, 0f), new Vector3(6.1f, 0.1f, 5.1f), wood, shop);
        if (med)
        {
            foreach (float x in new[] { -2f, 0f, 2f }) MedievalKit.Add(shop, "Wall_Plaster_Straight", new Vector3(x, 0.1f, -2.45f), 180f);
            foreach (float z in new[] { -1.5f, 0.5f }) MedievalKit.Add(shop, "Wall_Plaster_Straight", new Vector3(2.95f, 0.1f, z), 90f);
            foreach (var p in new[] { new Vector3(-3f, 0.1f, 2.45f), new Vector3(3f, 0.1f, 2.45f), new Vector3(3f, 0.1f, -2.5f), new Vector3(-3f, 0.1f, -2.5f) })
                MedievalKit.Add(shop, "Corner_Exterior_Wood", p, 0f);
        }
        else
        {
            Prim(PrimitiveType.Cube, "Pared", new Vector3(0f, 1.55f, -2.45f), new Vector3(6f, 3.1f, 0.3f), Mat("CTiendaPared", new Color(0.8f, 0.72f, 0.58f)), shop);
        }
        Solid(shop, "ColPared", new Vector3(0f, 1.55f, -2.5f), new Vector3(6f, 3.1f, 0.4f));
        Solid(shop, "ColLado", new Vector3(3f, 1.55f, -0.5f), new Vector3(0.4f, 3.1f, 4f));

        // techo inclinado hacia el frente + vigas + toldo de color
        var roof = Prim(PrimitiveType.Cube, "Techo", new Vector3(0f, 3.35f, 0f), new Vector3(6.8f, 0.22f, 6.2f), roofMat, shop);
        roof.transform.localRotation = Quaternion.Euler(9f, 0f, 0f);
        foreach (float x in new[] { -3f, 3f })
        {
            var vg = Prim(PrimitiveType.Cube, "Viga", new Vector3(x, 3.0f, 0f), new Vector3(0.2f, 0.2f, 5.6f), beam, shop, false);
            vg.transform.localRotation = Quaternion.Euler(9f, 0f, 0f);
        }
        var awn = Prim(PrimitiveType.Cube, "Toldo", new Vector3(0f, 2.55f, 2.95f), new Vector3(3.6f, 0.08f, 1.0f), cloth, shop, false);
        awn.transform.localRotation = Quaternion.Euler(22f, 0f, 0f);
        Prim(PrimitiveType.Cube, "Letrero", new Vector3(0f, 3.0f, 2.62f), new Vector3(2.6f, 0.5f, 0.1f), beam, shop, false);

        // mostrador
        const float TY = 1.0f;   // altura del mostrador (arriba)
        Put(shop, "Table_Large", new Vector3(0f, 0.1f, -1.4f), 0f, 1.15f, true);   // mostrador detras del vendedor
        const float S = 1.0f, SS = 1.9f;
        float cy = 0.1f + 0.93f;   // superficie del mostrador

        switch (vendor)
        {
            case "Herrero":
                Put(shop, "WeaponStand", new Vector3(1.8f, 0.1f, -1.6f), 0f, 1.3f, true);
                Put(shop, "Peg_Rack", new Vector3(-0.2f, 1.9f, -2.3f), 0f, 1.8f);
                Put(shop, "Shield_Wooden", new Vector3(-2.1f, 1.8f, -2.3f), 0f, 1.7f);
                Put(shop, "Shield_Wooden", new Vector3(2.1f, 1.8f, -2.3f), 0f, 1.7f);
                Put(shop, "Axe_Bronze", new Vector3(-1.1f, 1.5f, -2.3f), 0f, 1.6f);
                Put(shop, "Pickaxe_Bronze", new Vector3(1.0f, 1.5f, -2.3f), 0f, 1.5f);
                Put(shop, "Sword_Bronze", new Vector3(-1.2f, cy, -1.55f), 0f, 1.3f, false, 14f);
                Put(shop, "Sword_Bronze", new Vector3(-0.8f, cy, -1.55f), 0f, 1.3f, false, -10f);
                Put(shop, "Bucket_Metal", new Vector3(1.2f, cy, -1.50f), 0f, 1.6f);
                Put(shop, "Whetstone", new Vector3(3.9f, 0.1f, 0.6f), 90f, 1.1f, true);
                Put(shop, "Barrel", new Vector3(-2.4f, 0.1f, 0.2f), 0f, 1.1f, true);
                Put(shop, "Crate_Metal", new Vector3(-2.2f, 0.1f, -1.4f), 15f, 1.0f, true);
                break;
            case "Alquimista":
                Put(shop, "Cauldron", new Vector3(-1.9f, 0.1f, -1.2f), 0f, 1.2f, true);
                Put(shop, "BookStand", new Vector3(2.0f, 0.1f, -1.4f), 200f, 1.2f, true);
                Put(shop, "Shelf_Small_Bottles", new Vector3(0.2f, 1.5f, -2.3f), 0f, 1.7f);
                Put(shop, "Bookcase_2", new Vector3(2.0f, 0.1f, -2.0f), 0f, 0.8f, true);
                Put(shop, "Potion_2", new Vector3(-1.2f, cy, -1.50f), 0f, SS);
                Put(shop, "Potion_4", new Vector3(-0.6f, cy, -1.40f), 0f, SS);
                Put(shop, "Potion_1", new Vector3(0.0f, cy, -1.50f), 0f, SS);
                Put(shop, "Potion_2", new Vector3(0.6f, cy, -1.40f), 90f, SS);
                Put(shop, "SmallBottles_1", new Vector3(1.2f, cy, -1.50f), 30f, SS);
                Put(shop, "CandleStick_Triple", new Vector3(1.4f, cy, -1.10f), 0f, 1.8f);
                Put(shop, "Scroll_1", new Vector3(-1.4f, cy, -1.10f), 25f, SS);
                Put(shop, "Vase_2", new Vector3(3.9f, 0.1f, 0.8f), 0f, 1.0f);
                Put(shop, "CandleStick_Stand", new Vector3(-2.5f, 0.1f, 1.6f), 0f, 1.0f);
                break;
            default:   // Mercader
                Put(shop, "Crate_Wooden", new Vector3(-2.1f, 0.1f, -1.5f), 10f, 1.0f, true);
                Put(shop, "FarmCrate_Apple", new Vector3(-2.1f, 1.35f, -1.5f), 10f, 1.0f);
                Put(shop, "Barrel_Apples", new Vector3(2.0f, 0.1f, -1.6f), 0f, 1.1f, true);
                Put(shop, "Chest_Wood", new Vector3(2.0f, 0.1f, -0.3f), -90f, 1.0f, true);
                Put(shop, "FarmCrate_Carrot", new Vector3(-2.3f, 0.1f, -0.1f), 0f, 1.0f, true);
                Put(shop, "Coin_Pile_2", new Vector3(-1.1f, cy, -1.50f), 0f, 3.5f);
                Put(shop, "Coin_Pile", new Vector3(-0.4f, cy, -1.40f), 0f, 3.5f);
                Put(shop, "Pouch_Large", new Vector3(0.4f, cy, -1.50f), 40f, 2.0f);
                Put(shop, "Chalice", new Vector3(1.0f, cy, -1.40f), 0f, 2.0f);
                Put(shop, "Mug", new Vector3(1.4f, cy, -1.60f), 60f, 2.0f);
                Put(shop, "Bag", new Vector3(3.9f, 0.1f, 1.0f), 20f, 1.0f);
                Put(shop, "Bag", new Vector3(3.6f, 0.1f, 1.9f), 100f, 0.9f);
                Put(shop, "Rope_3", new Vector3(-3.4f, 0.12f, 2.0f), 0f, 1.0f);
                break;
        }
    }

    // Modelo 3D del vendedor (Assets/Modelos3D/Vendedores/<Nombre>/<Nombre>.fbx|obj). Devuelve false si no existe.
    internal static bool PlaceVendorModel(Transform shop, string vendor, Vector3 lp, float yaw, float height)
    {
        string folder = "Assets/Modelos3D/Vendedores/" + vendor + "/";
        var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(folder + vendor + ".fbx")
                     ?? AssetDatabase.LoadAssetAtPath<GameObject>(folder + vendor + ".obj");
        if (prefab == null) return false;

        var holder = new GameObject("Vendedor_" + vendor);
        holder.transform.SetParent(shop, false);
        var go = (GameObject)PrefabUtility.InstantiatePrefab(prefab);
        go.transform.SetParent(holder.transform, false);

        // sacar suelo / planos de chispas / sombras
        foreach (var r in go.GetComponentsInChildren<Renderer>(true))
        {
            string n = r.gameObject.name;
            if (n.StartsWith("Plane") || n.StartsWith("Spark") || n.StartsWith("Shadow"))
                r.gameObject.SetActive(false);
        }

        // materiales por nombre de pieza
        Texture2D body = AssetDatabase.LoadAssetAtPath<Texture2D>(folder + (vendor == "Herrero" ? "BLACKSMITH_TEX.jpg" : "L1png.png"));
        Texture2D anvil = AssetDatabase.LoadAssetAtPath<Texture2D>(folder + "Anvil_Tex.jpg");
        var bodyMat = TexMat("Vend_" + vendor, body, vendor == "Alquimista");
        var anvilMat = anvil != null ? TexMat("Vend_Yunque", anvil, false) : bodyMat;
        foreach (var r in go.GetComponentsInChildren<Renderer>(true))
        {
            if (!r.gameObject.activeSelf) continue;
            var m = r.gameObject.name.Contains("Anvil") ? anvilMat : bodyMat;
            var arr = new Material[Mathf.Max(1, r.sharedMaterials.Length)];
            for (int i = 0; i < arr.Length; i++) arr[i] = m;
            r.sharedMaterials = arr;
        }

        // si viene acostado (el eje largo es Z), se endereza
        Bounds b = Measure(go);
        if (b.size.z > b.size.y * 1.3f && b.size.z >= b.size.x) go.transform.localRotation = Quaternion.Euler(-90f, 0f, 0f) * go.transform.localRotation;
        b = Measure(go);
        if (b.size.y > 0.0001f) holder.transform.localScale = Vector3.one * (height / b.size.y);
        b = Measure(go);
        // pies en el suelo y centrado
        holder.transform.localPosition = lp;
        holder.transform.localRotation = Quaternion.Euler(0f, yaw, 0f);
        b = Measure(go);
        holder.transform.position += new Vector3(holder.transform.parent.TransformPoint(lp).x - b.center.x, holder.transform.parent.TransformPoint(lp).y - b.min.y, holder.transform.parent.TransformPoint(lp).z - b.center.z);

        b = Measure(go);
        var bc = holder.AddComponent<BoxCollider>();
        bc.center = holder.transform.InverseTransformPoint(b.center);
        Vector3 ls = holder.transform.lossyScale;
        bc.size = new Vector3(b.size.x / ls.x, b.size.y / ls.y, b.size.z / ls.z) * 0.8f;
        return true;
    }

    static Bounds Measure(GameObject go)
    {
        Bounds b = new Bounds(go.transform.position, Vector3.zero);
        bool first = true;
        foreach (var r in go.GetComponentsInChildren<Renderer>())
        {
            if (!r.gameObject.activeInHierarchy) continue;
            if (first) { b = r.bounds; first = false; } else b.Encapsulate(r.bounds);
        }
        return b;
    }

    static Material TexMat(string name, Texture2D tex, bool cutout)
    {
        string path = Dir + "Materials/" + name + ".mat";
        var m = AssetDatabase.LoadAssetAtPath<Material>(path);
        if (m == null)
        {
            m = new Material(Shader.Find("Universal Render Pipeline/Lit") ?? Shader.Find("Standard")) { name = name };
            AssetDatabase.CreateAsset(m, path);
        }
        m.SetFloat("_Smoothness", 0.15f);
        if (tex != null) { m.SetTexture("_BaseMap", tex); m.SetTexture("_MainTex", tex); m.color = Color.white; if (m.HasProperty("_BaseColor")) m.SetColor("_BaseColor", Color.white); }
        if (cutout) { m.SetFloat("_Cull", 0f); }
        EditorUtility.SetDirty(m);
        return m;
    }
}
