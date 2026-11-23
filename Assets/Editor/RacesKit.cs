using System.IO;
using UnityEditor;
using UnityEngine;
using static ZonaPruebasBuilder;

// Characters - Fantasy Races Kit (Quaternius, CC0): Elf_Male/Female, Orc_Male/Female.
// Carpeta: Assets/Modelos3D/Razas/{FBX,Textures}. Comparten esqueleto con la Universal Animation Library,
// asi que usan sus animaciones (Humanoid) a traves del controlador UAL.
internal static class RacesKit
{
    const string Dir = "Assets/Modelos3D/Razas/";
    static readonly string[] Races = { "Elf_Male", "Elf_Female", "Orc_Male", "Orc_Female" };

    internal static bool Available => AssetDatabase.LoadAssetAtPath<GameObject>(Dir + "FBX/Elf_Male.fbx") != null && UalKit.Available;

    [MenuItem("Certamen2/9 - Preparar razas fantasticas")]
    internal static void Prepare()
    {
        if (!Directory.Exists(Dir + "FBX")) return;
        foreach (var f in Directory.GetFiles(Dir + "Textures", "*.png"))
        {
            var ti = AssetImporter.GetAtPath(f.Replace('\\', '/')) as TextureImporter;
            if (ti != null && ti.maxTextureSize != 1024) { ti.maxTextureSize = 1024; ti.SaveAndReimport(); }
        }
        EnsureFolder("Assets/Modelos3D/Razas", "Materials");
        foreach (var race in Races)
        {
            string p = Dir + "FBX/" + race + ".fbx";
            var imp = AssetImporter.GetAtPath(p) as ModelImporter;
            if (imp == null) continue;
            imp.bakeAxisConversion = true;
            imp.animationType = ModelImporterAnimationType.Human;
            imp.avatarSetup = ModelImporterAvatarSetup.CreateFromThisModel;
            foreach (var o in AssetDatabase.LoadAllAssetsAtPath(p))
                if (o is Material m)
                    imp.AddRemap(new AssetImporter.SourceAssetIdentifier(typeof(Material), m.name), MatFor(m.name, 1));
            imp.SaveAndReimport();
        }
        AssetDatabase.SaveAssets();
        MakeEnemyPrefabs();
    }

    // Prefabs en Resources/Enemigos para que RunManager los instancie en ejecucion.
    static void MakeEnemyPrefabs()
    {
        if (!Available || !Directory.Exists(Dir + "FBX") || AssetDatabase.LoadAssetAtPath<GameObject>(Dir + "FBX/Orc_Male.fbx") == null) return;
        EnsureFolder("Assets/Resources", "Enemigos");
        Make("Orco", "Orc_Male", 1, 1.05f, 3, 2.2f, 2);
        Make("OrcoBruto", "Orc_Male", 3, 1.35f, 7, 1.7f, 4);
    }

    static void Make(string name, string race, int variant, float scale, int hp, float speed, int coins)
    {
        var go = Spawn(null, name, race, Vector3.zero, 0f, variant, "Sword_Idle", true, scale);
        if (go == null) return;
        go.GetComponent<ActorAnim>().manual = true;
        var e = go.AddComponent<Enemy>();
        e.hp = hp; e.speed = speed; e.coins = coins; e.label = name == "Orco" ? "Orco" : "Orco bruto";
        PrefabUtility.SaveAsPrefabAsset(go, "Assets/Resources/Enemigos/" + name + ".prefab");
        Object.DestroyImmediate(go);
    }

    static Texture2D TexFor(string matName, int v)
    {
        string t;
        if (matName.Contains("Eyes")) t = "T_Eye_" + matName.Substring(matName.LastIndexOf('_') + 1);
        else if (matName.Contains("Hair")) t = "T_FantasyHair_BaseColor_" + v;
        else t = "T_" + matName.Substring(3) + "_BaseColor_" + v;      // MI_Elf_Male -> T_Elf_Male_BaseColor_v
        return AssetDatabase.LoadAssetAtPath<Texture2D>(Dir + "Textures/" + t + ".png");
    }

    static Material MatFor(string matName, int v)
    {
        if (matName.StartsWith("R_")) matName = matName.Substring(2);
        int k = matName.LastIndexOf("_v");
        if (k > 0) matName = matName.Substring(0, k);
        bool eyes = matName.Contains("Eyes");
        string path = Dir + "Materials/R_" + matName + "_v" + (eyes ? 1 : v) + ".mat";
        var m = AssetDatabase.LoadAssetAtPath<Material>(path);
        if (m == null)
        {
            m = new Material(Shader.Find("Universal Render Pipeline/Lit") ?? Shader.Find("Standard")) { name = "R_" + matName + "_v" + (eyes ? 1 : v) };
            AssetDatabase.CreateAsset(m, path);
        }
        var tex = TexFor(matName, eyes ? 1 : v);
        if (tex != null)
        {
            m.SetTexture("_BaseMap", tex);
            m.SetTexture("_MainTex", tex);
        }
        m.color = Color.white;
        if (m.HasProperty("_BaseColor")) m.SetColor("_BaseColor", Color.white);
        m.SetFloat("_Smoothness", eyes ? 0.7f : 0.2f);
        EditorUtility.SetDirty(m);
        return m;
    }

    // race: "Elf_Male", "Elf_Female", "Orc_Male", "Orc_Female". variant 1-3 = textura de piel/pelo.
    internal static GameObject Spawn(Transform parent, string name, string race, Vector3 localPos, float yaw, int variant, string idle, bool collider, float scale = 1f)
    {
        var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(Dir + "FBX/" + race + ".fbx");
        var ctrl = AssetDatabase.LoadAssetAtPath<RuntimeAnimatorController>("Assets/Resources/Animaciones/UAL.controller");
        if (prefab == null || ctrl == null) return null;

        var holder = new GameObject(name);
        holder.transform.SetParent(parent, false);
        holder.transform.localPosition = localPos;
        holder.transform.localRotation = Quaternion.Euler(0f, yaw, 0f);
        holder.transform.localScale = Vector3.one * scale;
        var go = (GameObject)PrefabUtility.InstantiatePrefab(prefab);
        go.name = "Cuerpo";
        go.transform.SetParent(holder.transform, false);
        go.transform.localPosition = Vector3.zero;
        go.transform.localRotation = Quaternion.identity;

        var an = go.GetComponent<Animator>();
        if (an == null) an = go.AddComponent<Animator>();
        an.runtimeAnimatorController = ctrl;
        an.applyRootMotion = false;

        foreach (var r in go.GetComponentsInChildren<Renderer>(true))
        {
            if (race.StartsWith("Elf") && r.gameObject.name.EndsWith("Horns")) { r.gameObject.SetActive(false); continue; }
            if (variant != 1)
            {
                var arr = r.sharedMaterials;
                for (int i = 0; i < arr.Length; i++) if (arr[i] != null) arr[i] = MatFor(arr[i].name, variant);
                r.sharedMaterials = arr;
            }
        }
        // (ropa abandonada) if (ClothingKit.Available)
        //    ClothingKit.Dress(go, new Color(0.25f, 0.16f, 0.1f), new Color(0.2f, 0.13f, 0.08f));
        holder.AddComponent<ActorAnim>().idleClip = idle;
        if (collider)
        {
            var cc = holder.AddComponent<CapsuleCollider>();
            cc.radius = 0.4f; cc.height = 1.9f; cc.center = new Vector3(0f, 0.95f, 0f);
        }
        return holder;
    }
}
