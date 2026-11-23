using System.IO;
using UnityEditor;
using UnityEngine;
using static ZonaPruebasBuilder;

// Zombie guerrero (carpeta "malos juego"): 4 piezas (cuerpo, cabeza, armadura, arma) sin esqueleto.
// Archivos: Assets/Modelos3D/Malos/{body,head,armor,weapon}.fbx + Textures/<pieza>_Base_Color.png / _Normal.png
// Crea los prefabs Resources/Enemigos/Zombi y ZombiBruto (se animan por codigo: bamboleo, inclinacion, golpe).
internal static class ZombieKit
{
    const string Dir = "Assets/Modelos3D/Malos/";
    static readonly string[] Parts = { "body", "head", "armor", "weapon" };

    internal static bool Available => AssetDatabase.LoadAssetAtPath<GameObject>(Dir + "body.fbx") != null;

    [MenuItem("Certamen2/10 - Preparar zombis")]
    internal static void Prepare()
    {
        if (!Directory.Exists(Dir)) return;
        foreach (var part in Parts)
        {
            var imp = AssetImporter.GetAtPath(Dir + part + ".fbx") as ModelImporter;
            if (imp != null)
            {
                imp.materialImportMode = ModelImporterMaterialImportMode.None;
                imp.importAnimation = false;
                imp.SaveAndReimport();
            }
            foreach (var kind in new[] { "Base_Color", "Normal" })
            {
                string tp = Dir + "Textures/" + part + "_" + kind + ".png";
                var ti = AssetImporter.GetAtPath(tp) as TextureImporter;
                if (ti == null) continue;
                ti.maxTextureSize = 1024;
                ti.textureType = kind == "Normal" ? TextureImporterType.NormalMap : TextureImporterType.Default;
                ti.SaveAndReimport();
            }
        }
        EnsureFolder("Assets", "Resources");
        EnsureFolder("Assets/Resources", "Enemigos");
        Make("Zombi", 1f, 3, 2.0f, 2);
        Make("ZombiBruto", 1.3f, 7, 1.6f, 4);
        AssetDatabase.SaveAssets();
    }

    static Material MatFor(string part)
    {
        EnsureFolder("Assets/Modelos3D/Malos", "Materials");
        string path = Dir + "Materials/Mal_" + part + ".mat";
        var m = AssetDatabase.LoadAssetAtPath<Material>(path);
        if (m == null)
        {
            m = new Material(Shader.Find("Universal Render Pipeline/Lit") ?? Shader.Find("Standard"));
            AssetDatabase.CreateAsset(m, path);
        }
        var bc = AssetDatabase.LoadAssetAtPath<Texture2D>(Dir + "Textures/" + part + "_Base_Color.png");
        var nm = AssetDatabase.LoadAssetAtPath<Texture2D>(Dir + "Textures/" + part + "_Normal.png");
        m.color = Color.white;
        if (bc != null) { m.SetTexture("_BaseMap", bc); m.SetTexture("_MainTex", bc); }
        if (m.HasProperty("_BaseColor")) m.SetColor("_BaseColor", Color.white);
        if (nm != null) { m.SetTexture("_BumpMap", nm); m.EnableKeyword("_NORMALMAP"); m.SetFloat("_BumpScale", 1f); }
        m.SetFloat("_Smoothness", 0.15f);
        EditorUtility.SetDirty(m);
        return m;
    }

    static void Make(string name, float scale, int hp, float speed, int coins)
    {
        var holder = new GameObject(name);
        var model = new GameObject("Modelo").transform;
        model.SetParent(holder.transform, false);

        Renderer bodyR = null;
        GameObject weaponInst = null;
        var bodyRends = new System.Collections.Generic.List<Renderer>();
        foreach (var part in Parts)
        {
            var fbx = AssetDatabase.LoadAssetAtPath<GameObject>(Dir + part + ".fbx");
            if (fbx == null) continue;
            var inst = (GameObject)PrefabUtility.InstantiatePrefab(fbx);
            inst.name = part;
            inst.transform.SetParent(model, false);
            if (part == "weapon")
            {
                PrefabUtility.UnpackPrefabInstance(inst, PrefabUnpackMode.Completely, InteractionMode.AutomatedAction);
                weaponInst = inst;
            }
            var mat = MatFor(part);
            foreach (var r in inst.GetComponentsInChildren<Renderer>(true))
            {
                var arr = new Material[Mathf.Max(1, r.sharedMaterials.Length)];
                for (int i = 0; i < arr.Length; i++) arr[i] = mat;
                r.sharedMaterials = arr;
                if (part == "body" && bodyR == null) bodyR = r;
                if (part == "body") bodyRends.Add(r);
            }
        }
        if (weaponInst != null && bodyRends.Count > 0) FixWeapons(weaponInst, bodyRends);

        // escala a ~1.9 m de alto y apoya los pies en y=0 (centrado en X/Z segun el cuerpo)
        Bounds b = default; bool first = true;
        foreach (var r in model.GetComponentsInChildren<Renderer>(true))
        {
            if (first) { b = r.bounds; first = false; } else b.Encapsulate(r.bounds);
        }
        if (!first && b.size.y > 0.001f)
        {
            float k = 1.9f / b.size.y;
            model.localScale = Vector3.one * k;
            Bounds nb = default; bool f2 = true;
            foreach (var r in model.GetComponentsInChildren<Renderer>(true))
            {
                if (f2) { nb = r.bounds; f2 = false; } else nb.Encapsulate(r.bounds);
            }
            Vector3 c = bodyR != null ? bodyR.bounds.center : nb.center;
            model.localPosition = new Vector3(-c.x, -nb.min.y, -c.z);
        }

        holder.transform.localScale = Vector3.one * scale;
        var e = holder.AddComponent<Enemy>();
        e.hp = hp; e.speed = speed; e.coins = coins; e.label = name == "Zombi" ? "Zombi" : "Zombi bruto";
        PrefabUtility.SaveAsPrefabAsset(holder, "Assets/Resources/Enemigos/" + name + ".prefab");
        Object.DestroyImmediate(holder);
    }

    // ---- reacomoda el arma: escudo sobre el antebrazo, espada en la mano, quita piezas sueltas ----
    // Coordenadas originales del modelo (cm): cuerpo x +-58.7, y 18..151, z -20..17; escudo en x>0; espadas en el cinturon.
    static readonly Vector3 FMin = new Vector3(-58.68f, 18.43f, -20.26f), FMax = new Vector3(58.68f, 150.6f, 17.41f);

    static void FixWeapons(GameObject weapon, System.Collections.Generic.List<Renderer> bodyRends)
    {
        Bounds ub = bodyRends[0].bounds;
        foreach (var r in bodyRends) ub.Encapsulate(r.bounds);
        if (ub.size.y < ub.size.z || ub.size.y < ub.size.x * 0.5f) { Debug.LogWarning("ZombieKit: ejes inesperados, no se reacomoda el arma"); return; }

        var wr = weapon.GetComponentsInChildren<Renderer>(true);
        Renderer big = null; float bv = 0f;
        foreach (var r in wr) { float v = r.bounds.size.x * r.bounds.size.y * r.bounds.size.z; if (v > bv) { bv = v; big = r; } }
        if (big == null) return;
        bool fx = big.bounds.center.x < ub.center.x;   // el escudo esta en x>0 original
        bool fz = big.bounds.center.z < ub.center.z;   // y por delante del cuerpo (z>0)
        Vector3 umin = ub.min, umax = ub.max;

        System.Func<Vector3, Vector3> map = f =>
        {
            float tx = (f.x - FMin.x) / (FMax.x - FMin.x), ty = (f.y - FMin.y) / (FMax.y - FMin.y), tz = (f.z - FMin.z) / (FMax.z - FMin.z);
            if (fx) tx = 1f - tx;
            if (fz) tz = 1f - tz;
            return new Vector3(Mathf.Lerp(umin.x, umax.x, tx), Mathf.Lerp(umin.y, umax.y, ty), Mathf.Lerp(umin.z, umax.z, tz));
        };
        System.Func<Vector3, Vector3> inv = u =>
        {
            float tx = (u.x - umin.x) / (umax.x - umin.x), ty = (u.y - umin.y) / (umax.y - umin.y), tz = (u.z - umin.z) / (umax.z - umin.z);
            if (fx) tx = 1f - tx;
            if (fz) tz = 1f - tz;
            return new Vector3(Mathf.Lerp(FMin.x, FMax.x, tx), Mathf.Lerp(FMin.y, FMax.y, ty), Mathf.Lerp(FMin.z, FMax.z, tz));
        };
        System.Func<Vector3, Vector3> dir = v => map(v) - map(Vector3.zero);

        var shield = new System.Collections.Generic.List<Renderer>();
        var sword = new System.Collections.Generic.List<Renderer>();
        var junk = new System.Collections.Generic.List<Renderer>();
        foreach (var r in wr)
        {
            float cx = inv(r.bounds.center).x;
            if (cx > 100f) junk.Add(r);
            else if (cx > 30f) shield.Add(r);
            else if (cx > 12f) sword.Add(r);
        }
        foreach (var r in junk) Object.DestroyImmediate(r.gameObject);

        // escudo: se corre hacia el antebrazo para que parezca agarrado
        Vector3 sh = dir(new Vector3(-13f, 9f, 0f));
        foreach (var r in shield) r.transform.position += sh;

        // espada: pivote en la empunadura, se gira hacia adelante y se lleva a la mano libre (x<0)
        if (sword.Count > 0)
        {
            var pivot = new GameObject("EspadaMano").transform;
            pivot.SetParent(weapon.transform, true);
            pivot.position = map(new Vector3(21f, 98f, 6f));
            foreach (var r in sword) r.transform.SetParent(pivot, true);
            Quaternion q = Quaternion.FromToRotation(dir(new Vector3(1f, -30f, -50f)).normalized, dir(new Vector3(0f, 0.3f, 1f)).normalized);
            pivot.rotation = q * pivot.rotation;
            pivot.position = map(new Vector3(-56f, 103f, 6f));
        }
        Debug.Log("ZombieKit: arma reacomodada (flipX=" + fx + ", flipZ=" + fz + ", escudo=" + shield.Count + ", espada=" + sword.Count + ", sueltas=" + junk.Count + ")");
    }
}
