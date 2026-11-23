using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEngine;
using static ZonaPruebasBuilder;

// Ropa rigida para los personajes de razas: botas (a los pies) y cinturon (a la cadera), tomados de Medival.fbx.
// Se pegan a los huesos humanoides; se ajustan solos de tamano y orientacion. Sin camisa/pantalon (necesitan skinning).
// Archivo: Assets/Modelos3D/Ropa/Medival.fbx  (piezas: boots_left, Boots_right, bend = cinturon).
internal static class ClothingKit
{
    const string Fbx = "Assets/Modelos3D/Ropa/Medival.fbx";

    internal static bool Available => AssetDatabase.LoadAssetAtPath<GameObject>(Fbx) != null;

    [MenuItem("Certamen2/11 - Preparar ropa")]
    internal static void Prepare()
    {
        var imp = AssetImporter.GetAtPath(Fbx) as ModelImporter;
        if (imp == null) return;
        imp.materialImportMode = ModelImporterMaterialImportMode.None;   // sin las texturas incrustadas (pesadas)
        imp.importAnimation = false;
        imp.bakeAxisConversion = true;
        imp.SaveAndReimport();
    }

    static Material Leather(Color c, string name)
    {
        EnsureFolder("Assets/Modelos3D", "Ropa");
        EnsureFolder("Assets/Modelos3D/Ropa", "Materials");
        string path = "Assets/Modelos3D/Ropa/Materials/" + name + ".mat";
        var m = AssetDatabase.LoadAssetAtPath<Material>(path);
        if (m == null)
        {
            m = new Material(Shader.Find("Universal Render Pipeline/Lit") ?? Shader.Find("Standard"));
            AssetDatabase.CreateAsset(m, path);
        }
        m.color = c;
        if (m.HasProperty("_BaseColor")) m.SetColor("_BaseColor", c);
        m.SetFloat("_Smoothness", 0.25f);
        EditorUtility.SetDirty(m);
        return m;
    }

    static Transform Bone(Animator an, HumanBodyBones b)
    {
        if (an == null || !an.isHuman) return null;
        return an.GetBoneTransform(b);
    }

    static Bounds Bnd(GameObject g)
    {
        Bounds b = default; bool f = true;
        foreach (var r in g.GetComponentsInChildren<Renderer>(true))
        {
            if (f) { b = r.bounds; f = false; } else b.Encapsulate(r.bounds);
        }
        return b;
    }

    // body = el modelo instanciado (con Animator humanoide). Devuelve true si puso algo.
    internal static bool Dress(GameObject body, Color boot, Color belt)
    {
        var fbx = AssetDatabase.LoadAssetAtPath<GameObject>(Fbx);
        if (fbx == null) return false;
        var an = body.GetComponent<Animator>();
        var lf = Bone(an, HumanBodyBones.LeftFoot); var rf = Bone(an, HumanBodyBones.RightFoot);
        var lt = Bone(an, HumanBodyBones.LeftToes); var rt = Bone(an, HumanBodyBones.RightToes);
        var hips = Bone(an, HumanBodyBones.Hips); var spine = Bone(an, HumanBodyBones.Spine);
        var lul = Bone(an, HumanBodyBones.LeftUpperLeg); var rul = Bone(an, HumanBodyBones.RightUpperLeg);
        if (lf == null || rf == null || hips == null) return false;

        Bounds bb = Bnd(body);
        float groundY = bb.min.y;
        Vector3 fwd = body.transform.forward; fwd.y = 0f; fwd.Normalize();
        var rootFolder = new GameObject("Ropa").transform;
        rootFolder.SetParent(body.transform, true);
        var leatherBoot = Leather(boot, "Ropa_Botas");
        var leatherBelt = Leather(belt, "Ropa_Cinturon");

        PlaceBoot(fbx, "boots_left", lf, lt, groundY, fwd, rootFolder, leatherBoot);
        PlaceBoot(fbx, "boots_right", rf, rt, groundY, fwd, rootFolder, leatherBoot);

        float legDist = (lul != null && rul != null) ? Vector3.Distance(lul.position, rul.position) : 0.25f;
        Vector3 waist = spine != null ? Vector3.Lerp(hips.position, spine.position, 0.55f) : hips.position + Vector3.up * 0.1f;
        PlaceBelt(fbx, "bend", hips, waist, legDist * 2.4f, rootFolder, leatherBelt);
        return true;
    }

    static GameObject Take(GameObject fbx, string nameLower, out GameObject temp)
    {
        temp = (GameObject)PrefabUtility.InstantiatePrefab(fbx);
        temp.transform.position = Vector3.zero;
        temp.transform.rotation = Quaternion.identity;
        foreach (var t in temp.GetComponentsInChildren<Transform>(true))
            if (t.name.ToLower() == nameLower) return t.gameObject;
        return null;
    }

    static void PlaceBoot(GameObject fbx, string key, Transform foot, Transform toes, float groundY, Vector3 fwd, Transform folder, Material mat)
    {
        var piece = Take(fbx, key, out var temp);
        if (piece == null) { Object.DestroyImmediate(temp); return; }
        var mf = piece.GetComponent<MeshFilter>();
        Bounds b = Bnd(piece);

        // eje largo de la bota (X o Z) y sentido: la punta es el extremo mas bajo en promedio (el talon sube a la caña)
        bool alongX = b.size.x > b.size.z;
        float sign = 1f;
        if (mf != null && mf.sharedMesh != null)
        {
            float hi = 0, lo = 0; int nh = 0, nl = 0;
            foreach (var v in mf.sharedMesh.vertices)
            {
                Vector3 w = piece.transform.TransformPoint(v);
                float a = alongX ? w.x - b.center.x : w.z - b.center.z;
                if (a >= 0) { hi += w.y; nh++; } else { lo += w.y; nl++; }
            }
            float avgHi = nh > 0 ? hi / nh : 0, avgLo = nl > 0 ? lo / nl : 0;
            sign = avgHi > avgLo ? -1f : 1f;      // el lado mas alto es el talon -> la punta va al otro lado
        }
        Vector3 toe = (alongX ? Vector3.right : Vector3.forward) * sign;
        float length = alongX ? b.size.x : b.size.z;

        Vector3 footPos = foot.position;
        Vector3 toePos = toes != null ? toes.position : footPos + fwd * 0.12f;
        float footLen = Mathf.Max(0.1f, Vector3.Distance(new Vector3(footPos.x, 0, footPos.z), new Vector3(toePos.x, 0, toePos.z)) * 1.9f);
        float s = Mathf.Clamp(footLen * 1.2f / Mathf.Max(0.01f, length), 0.2f, 4f);

        var holder = new GameObject("Bota_" + key).transform;
        holder.position = b.center;
        piece.transform.SetParent(holder, true);
        holder.localScale = Vector3.one * s;
        holder.rotation = Quaternion.FromToRotation(toe, fwd);
        // apoyada en el suelo, centrada en el pie
        Bounds nb = Bnd(holder.gameObject);
        Vector3 center = (footPos + toePos) * 0.5f;
        holder.position += new Vector3(center.x - nb.center.x, groundY - nb.min.y, center.z - nb.center.z);
        holder.SetParent(foot, true);
        holder.SetParent(foot, true);
        Finish(piece, mat, temp, holder, folder);
    }

    static void PlaceBelt(GameObject fbx, string key, Transform hips, Vector3 waist, float targetWidth, Transform folder, Material mat)
    {
        var piece = Take(fbx, key, out var temp);
        if (piece == null) { Object.DestroyImmediate(temp); return; }
        Bounds b = Bnd(piece);
        float w = Mathf.Max(b.size.x, b.size.z);
        float s = Mathf.Clamp(targetWidth / Mathf.Max(0.01f, w), 0.2f, 4f);
        var holder = new GameObject("Cinturon").transform;
        holder.position = b.center;
        piece.transform.SetParent(holder, true);
        holder.localScale = Vector3.one * s;
        holder.position += waist - Bnd(holder.gameObject).center;
        holder.SetParent(hips, true);
        Finish(piece, mat, temp, holder, folder);
    }

    static void Finish(GameObject piece, Material mat, GameObject temp, Transform holder, Transform folder)
    {
        foreach (var r in piece.GetComponentsInChildren<Renderer>(true))
        {
            var arr = new Material[Mathf.Max(1, r.sharedMaterials.Length)];
            for (int i = 0; i < arr.Length; i++) arr[i] = mat;
            r.sharedMaterials = arr;
        }
        foreach (var c in piece.GetComponentsInChildren<Collider>(true)) Object.DestroyImmediate(c);
        Object.DestroyImmediate(temp);   // el resto del FBX (camisa, pantalon...) se descarta
    }
}
