using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEngine;
using static ZonaPruebasBuilder;

// Packs "Ultimate Stylized Nature" (Quaternius) y "RPG Asset Pack" (props): materiales URP + decoracion.
// Carpetas: Assets/Modelos3D/Naturaleza/{FBX,Textures}  y  Assets/Modelos3D/RPG/{FBX,Textures}
internal static class NatureKit
{
    const string ND = "Assets/Modelos3D/Naturaleza/";
    const string RD = "Assets/Modelos3D/RPG/";

    internal static bool Available => AssetDatabase.LoadAssetAtPath<GameObject>(ND + "FBX/NormalTree_1.fbx") != null;
    internal static bool RpgAvailable => AssetDatabase.LoadAssetAtPath<GameObject>(RD + "FBX/Sword.fbx") != null;

    [MenuItem("Certamen2/6 - Preparar naturaleza y RPG (materiales)")]
    static void PrepareMenu()
    {
        if (!Available && !RpgAvailable) { EditorUtility.DisplayDialog("Naturaleza / RPG", "No encuentro Assets/Modelos3D/Naturaleza ni RPG.", "OK"); return; }
        Prepare();
        EditorUtility.DisplayDialog("Naturaleza / RPG", "Materiales listos. Ahora ejecuta Certamen2 > 0 - Construir TODO.", "OK");
    }

    internal static void Prepare()
    {
        AssetDatabase.Refresh();
        PrepareFolder(ND, "Assets/Modelos3D/Naturaleza", false);
        PrepareFolder(RD, "Assets/Modelos3D/RPG", true);
        MakeSlotPrefabs();
    }

    static void PrepareFolder(string dir, string root, bool rpg)
    {
        if (!Directory.Exists(dir + "FBX")) return;
        if (Directory.Exists(dir + "Textures"))
            foreach (var f in Directory.GetFiles(dir + "Textures"))
            {
                if (f.EndsWith(".meta")) continue;
                var ti = AssetImporter.GetAtPath(f.Replace('\\', '/')) as TextureImporter;
                if (ti == null) continue;
                if (ti.maxTextureSize != 1024) { ti.maxTextureSize = 1024; ti.SaveAndReimport(); }
            }
        EnsureFolder(root, "Materials");
        var cache = new Dictionary<string, Material>();
        foreach (var raw in Directory.GetFiles(dir + "FBX", "*.fbx"))
        {
            string p = raw.Replace('\\', '/');
            string asset = Path.GetFileNameWithoutExtension(p);
            var imp = AssetImporter.GetAtPath(p) as ModelImporter;
            if (imp == null) continue;
            bool dirty = false;
            if (!imp.bakeAxisConversion) { imp.bakeAxisConversion = true; dirty = true; }
            foreach (var o in AssetDatabase.LoadAllAssetsAtPath(p))
            {
                if (!(o is Material m)) continue;
                string key = rpg ? asset + "_" + m.name : m.name;
                if (!cache.TryGetValue(key, out var mat)) { mat = rpg ? RpgMaterial(asset, m.name) : NatureMaterial(m.name); cache[key] = mat; }
                imp.AddRemap(new AssetImporter.SourceAssetIdentifier(typeof(Material), m.name), mat);
                dirty = true;
            }
            if (dirty) imp.SaveAndReimport();
        }
        AssetDatabase.SaveAssets();
        Debug.Log("Kit " + root + ": " + cache.Count + " materiales listos.");
    }

    static Shader Lit() => Shader.Find("Universal Render Pipeline/Lit") ?? Shader.Find("Standard");

    static Texture2D Tex(string dir, params string[] names)
    {
        foreach (var n in names)
            foreach (var ext in new[] { ".png", ".jpg" })
            {
                var t = AssetDatabase.LoadAssetAtPath<Texture2D>(dir + "Textures/" + n + ext);
                if (t != null) return t;
            }
        return null;
    }

    static Material NatureMaterial(string name)
    {
        string path = ND + "Materials/" + name + ".mat";
        var existing = AssetDatabase.LoadAssetAtPath<Material>(path);
        if (existing != null) return existing;

        string n = name.ToLowerInvariant();
        bool cut = n.Contains("leaves") || n.Contains("grass") || n.Contains("flower") || n.Contains("petal");
        Texture2D tex;
        if (n.Contains("rock")) tex = Tex(ND, "Rocks");
        else if (n.Contains("grass")) tex = Tex(ND, "Grass");
        else if (n.Contains("flower") || n.Contains("petal")) tex = Tex(ND, "Flowers");
        else tex = Tex(ND, name);

        var mat = new Material(Lit()) { name = name };
        mat.SetFloat("_Smoothness", 0.05f);
        mat.SetFloat("_Metallic", 0f);
        if (tex != null) { mat.SetTexture("_BaseMap", tex); mat.SetTexture("_MainTex", tex); }
        // tinte apagado: se ve descuidado / abandonado
        Color tint = Color.white;
        if (cut) tint = new Color(0.78f, 0.74f, 0.55f);
        else if (n.Contains("rock")) tint = new Color(0.8f, 0.8f, 0.82f);
        mat.color = tint;
        if (mat.HasProperty("_BaseColor")) mat.SetColor("_BaseColor", tint);
        if (cut)
        {
            mat.SetFloat("_AlphaClip", 1f);
            mat.SetFloat("_Cutoff", 0.5f);
            mat.EnableKeyword("_ALPHATEST_ON");
            mat.SetFloat("_Cull", 0f);
        }
        AssetDatabase.CreateAsset(mat, path);
        return mat;
    }

    static Material RpgMaterial(string asset, string name)
    {
        string path = RD + "Materials/" + asset + "_" + name + ".mat";
        var existing = AssetDatabase.LoadAssetAtPath<Material>(path);
        if (existing != null) return existing;

        string n = name.ToLowerInvariant();
        Texture2D tex = null;
        Color col = new Color(0.55f, 0.55f, 0.58f);
        float smooth = 0.2f, metal = 0f;
        Color emis = Color.black;

        switch (asset)
        {
            case "Book": tex = Tex(RD, "Book"); break;
            case "Dagger": tex = Tex(RD, "Dagger"); break;
            case "Sword": tex = Tex(RD, "Sword"); break;
            case "Scroll": tex = Tex(RD, "Scroll"); break;
            case "Rollofpaper": tex = Tex(RD, "Roll"); break;
            case "Gems":
                {
                    Color[] g = { new Color(0.9f, 0.15f, 0.2f), new Color(0.2f, 0.4f, 0.95f), new Color(0.2f, 0.85f, 0.35f), new Color(0.7f, 0.25f, 0.9f), new Color(0.95f, 0.85f, 0.2f), new Color(0.3f, 0.9f, 0.95f), new Color(0.95f, 0.95f, 1f) };
                    int idx = 0; int.TryParse(name.Substring(name.LastIndexOf('.') + 1), out idx);
                    col = g[Mathf.Abs(idx - 6) % g.Length]; smooth = 0.9f; emis = col * 0.4f; break;
                }
        }
        if (tex == null && asset != "Gems")
        {
            if (n.Contains("darkwood")) col = new Color(0.25f, 0.16f, 0.09f);
            else if (n.Contains("lighterwood")) col = new Color(0.6f, 0.42f, 0.22f);
            else if (n.Contains("wood")) col = new Color(0.42f, 0.28f, 0.14f);
            else if (n.Contains("darkersilver")) { col = new Color(0.4f, 0.4f, 0.45f); metal = 0.7f; smooth = 0.5f; }
            else if (n.Contains("silver") || n.Contains("metal")) { col = new Color(0.65f, 0.66f, 0.7f); metal = 0.8f; smooth = 0.6f; }
            else if (n.Contains("red")) col = new Color(0.65f, 0.1f, 0.1f);
            else if (n.Contains("eyes")) col = new Color(0.04f, 0.04f, 0.05f);
            else if (n.Contains("ice")) { col = new Color(0.55f, 0.85f, 1f); smooth = 0.9f; emis = new Color(0.1f, 0.35f, 0.5f); }
            else if (n.Contains("gem")) { col = new Color(0.75f, 0.2f, 0.95f); smooth = 0.9f; emis = new Color(0.35f, 0.08f, 0.5f); }
            else if (asset == "AnimatedChest")
            {
                if (name == "Material") col = new Color(0.45f, 0.28f, 0.13f);
                else if (name.EndsWith("001")) col = new Color(0.6f, 0.6f, 0.65f);
                else col = new Color(0.85f, 0.68f, 0.2f);
            }
        }

        var mat = new Material(Lit()) { name = asset + "_" + name };
        mat.SetFloat("_Smoothness", smooth);
        mat.SetFloat("_Metallic", metal);
        mat.color = col;
        if (mat.HasProperty("_BaseColor")) mat.SetColor("_BaseColor", col);
        if (tex != null) { mat.color = Color.white; if (mat.HasProperty("_BaseColor")) mat.SetColor("_BaseColor", Color.white); mat.SetTexture("_BaseMap", tex); mat.SetTexture("_MainTex", tex); }
        if (emis != Color.black) { mat.EnableKeyword("_EMISSION"); mat.SetColor("_EmissionColor", emis); }
        AssetDatabase.CreateAsset(mat, path);
        return mat;
    }

    // Prefabs en Resources/Modelos para los ModelSlot (Cofre, Altar)
    static void MakeSlotPrefabs()
    {
        if (!RpgAvailable) return;
        EnsureFolder("Assets", "Resources");
        EnsureFolder("Assets/Resources", "Modelos");
        SlotPrefab("Cofre", "AnimatedChest");
        SlotPrefab("Altar", "Gems");
    }

    static void SlotPrefab(string slot, string fbx)
    {
        string dst = "Assets/Resources/Modelos/" + slot + ".prefab";
        if (AssetDatabase.LoadAssetAtPath<GameObject>(dst) != null) return;   // no pisar el que puso el usuario
        var src = AssetDatabase.LoadAssetAtPath<GameObject>(RD + "FBX/" + fbx + ".fbx");
        if (src == null) return;
        var inst = (GameObject)PrefabUtility.InstantiatePrefab(src);
        PrefabUtility.SaveAsPrefabAsset(inst, dst);
        Object.DestroyImmediate(inst);
    }

    // ------------------------------------------------------------------ colocacion
    static GameObject Load(string dir, string piece) => AssetDatabase.LoadAssetAtPath<GameObject>(dir + "FBX/" + piece + ".fbx");

    // Pone una pieza (opcionalmente con colision de tronco/roca) y devuelve el contenedor
    internal static GameObject Place(Transform parent, bool rpg, string piece, Vector3 pos, float yaw, float scale, string collider = null)
    {
        var prefab = Load(rpg ? RD : ND, piece);
        if (prefab == null) return null;
        var holder = new GameObject(piece);
        holder.transform.SetParent(parent, false);
        holder.transform.position = pos;
        holder.transform.rotation = Quaternion.Euler(0f, yaw, 0f);
        holder.transform.localScale = Vector3.one * scale;
        var go = (GameObject)PrefabUtility.InstantiatePrefab(prefab);
        go.transform.SetParent(holder.transform, false);
        go.transform.localPosition = Vector3.zero;
        go.transform.localRotation = prefab.transform.localRotation;

        if (collider == "trunk")
        {
            var cc = holder.AddComponent<CapsuleCollider>();
            cc.radius = 0.4f; cc.height = 3.5f; cc.center = new Vector3(0f, 1.75f, 0f);
        }
        else if (collider == "box")
        {
            var rends = go.GetComponentsInChildren<Renderer>();
            if (rends.Length > 0)
            {
                Bounds b = rends[0].bounds;
                foreach (var r in rends) b.Encapsulate(r.bounds);
                var bc = holder.AddComponent<BoxCollider>();
                bc.center = holder.transform.InverseTransformPoint(b.center);
                bc.size = new Vector3(b.size.x / scale, b.size.y / scale, b.size.z / scale) * 0.85f;
            }
        }
        return holder;
    }

    internal static readonly string[] Trees = { "NormalTree_1", "NormalTree_2", "NormalTree_3", "NormalTree_5", "BirchTree_4", "MapleTree_4" };
    internal static readonly string[] Pines = { "PineTree_1", "PineTree_3", "PineTree_5" };
    internal static readonly string[] Dead = { "DeadTree_1", "DeadTree_2", "DeadTree_3", "DeadTree_4", "DeadTree_5", "DeadTree_6", "DeadTree_7", "DeadTree_8", "DeadTree_9", "DeadTree_10" };
    internal static readonly string[] Bushes = { "Bush", "Bush_Large", "Bush_Small", "Bush_Flowers" };
    internal static readonly string[] Grass = { "Grass_Large", "Grass_Small", "Grass_Large_Extruded" };
    internal static readonly string[] Flowers = { "Flower_1_Clump", "Flower_2_Clump", "Plant_1", "Plant_2", "Petals_1" };
    internal static readonly string[] Rocks = { "Rock_1", "Rock_2", "Rock_3", "Rock_4", "Rock_5" };

    // Reparte 'count' piezas al azar dentro de un rectangulo (x0..x1, z0..z1) a altura y.
    // Salta los puntos que chocan con algo ya construido o que 'reject' rechaza.
    internal static int Scatter(Transform parent, System.Random r, int count, float x0, float x1, float z0, float z1, float y,
                                string[] pieces, float sMin, float sMax, float clear, string collider, System.Func<Vector3, bool> reject = null)
    {
        int placed = 0;
        for (int i = 0; i < count; i++)
        {
            for (int tries = 0; tries < 12; tries++)
            {
                var p = new Vector3(Mathf.Lerp(x0, x1, (float)r.NextDouble()), y, Mathf.Lerp(z0, z1, (float)r.NextDouble()));
                if (reject != null && reject(p)) continue;
                Physics.SyncTransforms();
                if (Physics.CheckSphere(p + Vector3.up * (clear + 0.15f), clear, ~0, QueryTriggerInteraction.Ignore)) continue;
                string piece = pieces[r.Next(pieces.Length)];
                float s = Mathf.Lerp(sMin, sMax, (float)r.NextDouble());
                var h = Place(parent, false, piece, p, (float)r.NextDouble() * 360f, s, collider);
                if (h != null) placed++;
                break;
            }
        }
        return placed;
    }
}
