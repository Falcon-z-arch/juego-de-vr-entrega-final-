using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEngine;
using static ZonaPruebasBuilder;

// Kit "Medieval Village MegaKit" (Quaternius, CC0): prepara materiales URP y arma casas con las piezas.
// Carpeta esperada: Assets/Modelos3D/MedievalVillage/{FBX,Textures}
internal static class MedievalKit
{
    const string Dir = "Assets/Modelos3D/MedievalVillage/";

    internal static bool Available => AssetDatabase.LoadAssetAtPath<GameObject>(Dir + "FBX/Wall_Plaster_Straight.fbx") != null;

    [MenuItem("Certamen2/5 - Preparar kit medieval (materiales)")]
    static void PrepareMenu()
    {
        if (!Directory.Exists(Dir + "FBX")) { EditorUtility.DisplayDialog("Kit medieval", "No encuentro " + Dir + "FBX", "OK"); return; }
        Prepare();
        EditorUtility.DisplayDialog("Kit medieval", "Materiales listos. Ahora ejecuta Certamen2 > 0 - Construir TODO.", "OK");
    }

    // Texturas (normal map, 1024 px para el celular) + materiales URP compartidos asignados a cada FBX
    internal static void Prepare()
    {
        if (!Directory.Exists(Dir + "FBX")) return;
        AssetDatabase.Refresh();

        foreach (var f in Directory.GetFiles(Dir + "Textures", "*.png"))
        {
            var ti = AssetImporter.GetAtPath(f.Replace('\\', '/')) as TextureImporter;
            if (ti == null) continue;
            bool changed = false;
            if (f.EndsWith("_Normal.png") && ti.textureType != TextureImporterType.NormalMap) { ti.textureType = TextureImporterType.NormalMap; changed = true; }
            if (ti.maxTextureSize != 1024) { ti.maxTextureSize = 1024; changed = true; }
            if (changed) ti.SaveAndReimport();
        }

        EnsureFolder("Assets/Modelos3D/MedievalVillage", "Materials");
        var shared = new Dictionary<string, Material>();
        foreach (var raw in Directory.GetFiles(Dir + "FBX", "*.fbx"))
        {
            string p = raw.Replace('\\', '/');
            var imp = AssetImporter.GetAtPath(p) as ModelImporter;
            if (imp == null) continue;
            bool dirty = false;
            // los FBX de Blender llegan girados 90 grados en X: se corrige en el importador
            if (!imp.bakeAxisConversion) { imp.bakeAxisConversion = true; dirty = true; }
            foreach (var o in AssetDatabase.LoadAllAssetsAtPath(p))
            {
                if (!(o is Material m)) continue;
                if (!shared.TryGetValue(m.name, out var mat)) { mat = GetMaterial(m.name); shared[m.name] = mat; }
                imp.AddRemap(new AssetImporter.SourceAssetIdentifier(typeof(Material), m.name), mat);
                dirty = true;
            }
            if (dirty) imp.SaveAndReimport();
        }
        AssetDatabase.SaveAssets();
        Debug.Log("Kit medieval: " + shared.Count + " materiales listos.");
    }

    static Material GetMaterial(string name)
    {
        string path = Dir + "Materials/" + name + ".mat";
        var existing = AssetDatabase.LoadAssetAtPath<Material>(path);
        if (existing != null) return existing;

        string n = name.ToLowerInvariant(), color = null, normal = null;
        bool glass = n.Contains("glass"), leaf = false;
        if (glass) { }
        else if (n.Contains("uneven")) { color = "T_UnevenBrick_BaseColor"; normal = "T_UnevenBrick_Normal"; }
        else if (n.Contains("redbrick")) { color = "T_RedBrick_BaseColor"; normal = "T_Brick_Normal"; }
        else if (n.Contains("brick")) { color = "T_Brick_BaseColor"; normal = "T_Brick_Normal"; }
        else if (n.Contains("plaster")) { color = "T_Plaster_BaseColor"; normal = "T_Plaster_Normal"; }
        else if (n.Contains("wood")) { color = "T_WoodTrim_BaseColor"; normal = "T_WoodTrim_Normal"; }
        else if (n.Contains("rock")) { color = "T_RockTrim_BaseColor"; normal = "T_RockTrim_Normal"; }
        else if (n.Contains("round") || n.Contains("tile")) { color = "T_RoundTiles_BaseColor"; normal = "T_RoundTiles_Normal"; }
        else if (n.Contains("vine") || n.Contains("leaf")) { color = "T_VineLeaf"; leaf = true; }
        else Debug.LogWarning("Kit medieval: material sin textura conocida: " + name);

        var sh = Shader.Find("Universal Render Pipeline/Lit");
        if (sh == null) sh = Shader.Find("Standard");
        var mat = new Material(sh) { name = name };
        mat.SetFloat("_Smoothness", 0.15f);
        mat.SetFloat("_Metallic", 0f);

        if (glass)
        {
            var c = new Color(0.65f, 0.8f, 0.9f, 0.25f);
            mat.color = c;
            if (mat.HasProperty("_BaseColor")) mat.SetColor("_BaseColor", c);
            mat.SetFloat("_Surface", 1f);
            mat.SetFloat("_Blend", 0f);
            mat.SetOverrideTag("RenderType", "Transparent");
            mat.SetInt("_SrcBlend", 5);
            mat.SetInt("_DstBlend", 10);
            mat.SetInt("_ZWrite", 0);
            mat.EnableKeyword("_SURFACE_TYPE_TRANSPARENT");
            mat.renderQueue = 3000;
            mat.SetFloat("_Smoothness", 0.9f);
        }
        else if (color != null)
        {
            var tex = AssetDatabase.LoadAssetAtPath<Texture2D>(Dir + "Textures/" + color + ".png");
            if (tex != null) { mat.SetTexture("_BaseMap", tex); mat.SetTexture("_MainTex", tex); }
            if (normal != null)
            {
                var nt = AssetDatabase.LoadAssetAtPath<Texture2D>(Dir + "Textures/" + normal + ".png");
                if (nt != null) { mat.SetTexture("_BumpMap", nt); mat.EnableKeyword("_NORMALMAP"); }
            }
            if (leaf)
            {
                mat.SetFloat("_AlphaClip", 1f);
                mat.SetFloat("_Cutoff", 0.5f);
                mat.EnableKeyword("_ALPHATEST_ON");
                mat.SetFloat("_Cull", 0f);
            }
        }
        else mat.color = new Color(0.5f, 0.5f, 0.5f);

        AssetDatabase.CreateAsset(mat, path);
        return mat;
    }

    // ------------------------------------------------------------------ piezas
    internal static GameObject Add(Transform parent, string piece, Vector3 lp, float yaw)
    {
        var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(Dir + "FBX/" + piece + ".fbx");
        if (prefab == null) { Debug.LogWarning("Kit medieval: falta " + piece); return null; }
        var go = PrefabUtility.InstantiatePrefab(prefab) as GameObject;
        go.transform.SetParent(parent, false);
        go.transform.localPosition = lp;
        go.transform.localRotation = Quaternion.Euler(0f, yaw, 0f) * prefab.transform.localRotation; // respeta la rotacion -90 X del FBX
        return go;
    }

    // Casa de 6 x 8 m (3 x 4 modulos de 2 m). El frente (puerta) mira hacia +Z local; yaw 180 = mira hacia -Z.
    // style: "plaster" (enlucido blanco) o "brick" (ladrillo irregular).
    internal static GameObject House(string style, Vector3 pos, float yaw)
    {
        bool brick = style == "brick";
        string wall = brick ? "Wall_UnevenBrick_Straight" : "Wall_Plaster_Straight";
        string door = brick ? "Wall_UnevenBrick_Door_Round" : "Wall_Plaster_Door_Round";
        string win = brick ? "Wall_UnevenBrick_Window_Wide_Round" : "Wall_Plaster_Window_Wide_Round";

        var root = new GameObject(brick ? "CasaLadrillo" : "CasaEnlucida");
        root.transform.position = pos;
        root.transform.rotation = Quaternion.Euler(0f, yaw, 0f);
        var t = root.transform;

        // interior oscuro y solido (tapa el hueco de puertas y ventanas; tambien es la colision de la casa)
        var inner = Prim(PrimitiveType.Cube, "Interior", new Vector3(0f, 1.55f, 0f), new Vector3(5.2f, 3.1f, 7.2f), Mat("CCasaInterior", new Color(0.13f, 0.09f, 0.07f)), t);
        inner.transform.localPosition = new Vector3(0f, 1.55f, 0f);

        // frente (+Z): ventana, puerta, ventana
        Facade(t, new[] { win, door, win }, new[] { -2f, 0f, 2f }, 3.91f, 0f, true, true);
        // fondo (-Z)
        Facade(t, new[] { wall, win, wall }, new[] { -2f, 0f, 2f }, -3.91f, 180f, false, false);
        // lados (4 modulos cada uno)
        Side(t, wall, win, -2.91f, -90f);
        Side(t, wall, win, 2.91f, 90f);

        // postes de esquina
        foreach (float cx in new[] { -3f, 3f })
            foreach (float cz in new[] { -4f, 4f })
                Add(t, "Corner_Exterior_Wood", new Vector3(cx, 0f, cz), 0f);

        // techo a dos aguas (caballete a lo largo de Z) + frontones
        Add(t, "Roof_RoundTiles_6x8", new Vector3(0f, 3.12f, 0f), 0f);
        Add(t, "Roof_Front_Brick6", new Vector3(0f, 3.12f, 3.91f), 0f);
        Add(t, "Roof_Front_Brick6", new Vector3(0f, 3.12f, -3.91f), 180f);
        Add(t, "Prop_Chimney", new Vector3(2f, 4.0f, -1.5f), 0f);
        return root;
    }

    static void Facade(Transform t, string[] pieces, float[] xs, float z, float yaw, bool withDoor, bool shutters)
    {
        for (int i = 0; i < pieces.Length; i++)
        {
            var lp = new Vector3(xs[i], 0f, z);
            Add(t, pieces[i], lp, yaw);
            if (pieces[i].Contains("Window"))
            {
                Add(t, "Window_Wide_Round1", lp, yaw);
                if (shutters) Add(t, "WindowShutters_Wide_Round_Open", lp, yaw);
            }
            else if (pieces[i].Contains("Door"))
            {
                Add(t, "DoorFrame_Round_WoodDark", lp, yaw);
                Add(t, "Door_1_Round", lp + Quaternion.Euler(0f, yaw, 0f) * new Vector3(-0.55f, 0f, 0f), yaw);
            }
        }
    }

    static void Side(Transform t, string wall, string win, float x, float yaw)
    {
        string[] p = { wall, win, win, wall };
        float[] zs = { -3f, -1f, 1f, 3f };
        for (int i = 0; i < 4; i++)
        {
            var lp = new Vector3(x, 0f, zs[i]);
            Add(t, p[i], lp, yaw);
            if (p[i].Contains("Window")) Add(t, "Window_Wide_Round1", lp, yaw);
        }
    }

    // Props sueltos (cajas, carreta) con su colision
    internal static void Props()
    {

        var wagon = new GameObject("Carreta");
        wagon.transform.position = new Vector3(-40f, 0f, -6.5f);
        wagon.transform.rotation = Quaternion.Euler(0f, 90f, 0f);
        if (Add(wagon.transform, "Prop_Wagon", Vector3.zero, 0f) != null)
        {
            var bc = wagon.AddComponent<BoxCollider>();
            bc.center = new Vector3(0f, 0.75f, -1.14f); bc.size = new Vector3(1.9f, 1.5f, 4f);
        }
    }

    static void Crate(Vector3 pos, float yaw)
    {
        var c = new GameObject("Caja");
        c.transform.position = pos;
        c.transform.rotation = Quaternion.Euler(0f, yaw, 0f);
        if (Add(c.transform, "Prop_Crate", Vector3.zero, 0f) == null) { Object.DestroyImmediate(c); return; }
        var bc = c.AddComponent<BoxCollider>();
        bc.center = new Vector3(0f, 0.53f, 0f); bc.size = new Vector3(1.08f, 1.06f, 1.08f);
    }
}
