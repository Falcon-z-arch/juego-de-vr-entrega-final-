using System.Collections.Generic;
using UnityEngine;

// Disenos de los personajes: accesorios armados con formas simples que siguen los huesos del modelo.
// Hobbit: gorro verde, mochila con petate, bufanda.  Mago: sombrero con estrella, capa, baston con orbe.
// Ingeniero: antiparras, mochila con tanques, cinturon de herramientas.  Peleador: casco con cresta, hombreras, capa corta.
// Nombres: "AccH_*" = en la cabeza (se oculta en primera persona); "Acc_*" = cuerpo (se ve al mirar hacia abajo).
public static class CharacterDesign
{
    static Material baseMat;

    static Material M(Color c, float metal, float smooth, float emis)
    {
        if (baseMat == null)
        {
            var t = GameObject.CreatePrimitive(PrimitiveType.Cube);
            baseMat = t.GetComponent<Renderer>().sharedMaterial;
            Object.Destroy(t);
        }
        var m = new Material(baseMat) { color = c };
        if (m.HasProperty("_Metallic")) m.SetFloat("_Metallic", metal);
        if (m.HasProperty("_Smoothness")) m.SetFloat("_Smoothness", smooth);
        if (emis > 0f)
        {
            m.EnableKeyword("_EMISSION");
            if (m.HasProperty("_EmissionColor")) m.SetColor("_EmissionColor", c * emis);
        }
        return m;
    }

    class Ctx
    {
        public Transform root, holder;
        public Animator an;
        public float s;
        public Dictionary<string, Material> mats = new Dictionary<string, Material>();
        public Material Mat(Color c, float metal = 0f, float smooth = 0.3f, float emis = 0f)
        {
            string k = c.ToString() + metal + smooth + emis;
            if (!mats.TryGetValue(k, out var m)) { m = M(c, metal, smooth, emis); mats[k] = m; }
            return m;
        }
    }

    // pieza simple pegada a un hueso. off/euler en el marco del personaje (metros a escala 1)
    static GameObject Add(Ctx c, string name, HumanBodyBones bone, PrimitiveType t, Vector3 off, Vector3 scl, Vector3 euler, Material m, bool head = false)
    {
        var b = c.an.GetBoneTransform(bone);
        if (b == null) return null;
        var go = GameObject.CreatePrimitive(t);
        go.name = (head ? "AccH_" : "Acc_") + name;
        var col = go.GetComponent<Collider>(); if (col != null) Object.Destroy(col);
        go.transform.SetParent(c.holder, false);
        go.transform.localScale = scl * c.s;
        var r = go.GetComponent<Renderer>();
        r.sharedMaterial = m;
        r.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
        var f = go.AddComponent<BoneFollow>();
        f.bone = b; f.root = c.root; f.offset = off * c.s; f.rot = Quaternion.Euler(euler);
        return go;
    }

    // anillo de cubos (cinturon, bufanda, diadema) alrededor de un hueso
    static void Ring(Ctx c, string name, HumanBodyBones bone, Vector3 center, float rx, float rz, int n, Vector3 piece, Material m, bool head = false)
    {
        for (int i = 0; i < n; i++)
        {
            float a = i * Mathf.PI * 2f / n;
            Add(c, name, bone, PrimitiveType.Cube, center + new Vector3(Mathf.Sin(a) * rx, 0f, Mathf.Cos(a) * rz), piece,
                new Vector3(0f, a * Mathf.Rad2Deg, 0f), m, head);
        }
    }


    // prenda que cubre un segmento entre dos huesos (manga, pantalon, bota, torso): sigue el movimiento de las extremidades
    static void Limb(Ctx c, string name, HumanBodyBones a, HumanBodyBones b, float radius, Material m, float sx = 1f, float sz = 1f, float over = 1.06f, float shift = 0f)
    {
        var ta = c.an.GetBoneTransform(a); var tb = c.an.GetBoneTransform(b);
        if (ta == null || tb == null) return;
        var go = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
        go.name = "Acc_" + name;
        var col = go.GetComponent<Collider>(); if (col != null) Object.Destroy(col);
        go.transform.SetParent(c.holder, false);
        var r = go.GetComponent<Renderer>();
        r.sharedMaterial = m;
        r.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
        var f = go.AddComponent<LimbFollow>();
        f.a = ta; f.b = tb; f.radius = radius * c.s; f.sx = sx; f.sz = sz; f.over = over; f.shift = shift;
    }

    static void Clothes(Ctx c, Role role)
    {
        Color top, sleeve, pants, boots, trim;
        Clothes(c, role);

        switch (role)
        {
            case Role.Hobbit: top = new Color(0.30f, 0.52f, 0.22f); sleeve = new Color(0.93f, 0.88f, 0.72f); pants = new Color(0.45f, 0.30f, 0.16f); boots = new Color(0.28f, 0.17f, 0.09f); trim = new Color(0.85f, 0.7f, 0.2f); break;
            case Role.Mage: top = new Color(0.33f, 0.14f, 0.58f); sleeve = new Color(0.28f, 0.12f, 0.5f); pants = new Color(0.2f, 0.1f, 0.36f); boots = new Color(0.16f, 0.1f, 0.22f); trim = new Color(1f, 0.82f, 0.25f); break;
            case Role.Engineer: top = new Color(0.78f, 0.62f, 0.38f); sleeve = new Color(0.78f, 0.62f, 0.38f); pants = new Color(0.22f, 0.3f, 0.42f); boots = new Color(0.25f, 0.2f, 0.16f); trim = new Color(0.9f, 0.45f, 0.1f); break;
            default: top = new Color(0.72f, 0.12f, 0.12f); sleeve = new Color(0.2f, 0.2f, 0.22f); pants = new Color(0.17f, 0.17f, 0.2f); boots = new Color(0.4f, 0.42f, 0.46f); trim = new Color(0.55f, 0.58f, 0.62f); break;
        }
        var mTop = c.Mat(top, 0f, 0.15f); var mSl = c.Mat(sleeve, 0f, 0.15f); var mPa = c.Mat(pants, 0f, 0.15f);
        var mBo = c.Mat(boots, role == Role.Fighter ? 0.8f : 0f, role == Role.Fighter ? 0.6f : 0.2f); var mTr = c.Mat(trim, 0.5f, 0.4f);
        var UC = c.an.GetBoneTransform(HumanBodyBones.UpperChest) != null ? HumanBodyBones.UpperChest : HumanBodyBones.Chest;
        var Hb = HumanBodyBones.Hips;

        // torso (tunica / chaleco / tunica larga)
        Limb(c, "Torso", Hb, UC, 0.16f, mTop, 1.35f, 0.85f, 1.12f, 0.02f);
        Limb(c, "Pecho", UC, HumanBodyBones.Neck, 0.17f, mTop, 1.35f, 0.85f, 0.9f);
        // pantalon: muslo y pantorrilla
        Limb(c, "MusloI", HumanBodyBones.LeftUpperLeg, HumanBodyBones.LeftLowerLeg, 0.1f, mPa);
        Limb(c, "MusloD", HumanBodyBones.RightUpperLeg, HumanBodyBones.RightLowerLeg, 0.1f, mPa);
        Limb(c, "PiernaI", HumanBodyBones.LeftLowerLeg, HumanBodyBones.LeftFoot, 0.075f, mPa, 1f, 1f, 0.75f);
        Limb(c, "PiernaD", HumanBodyBones.RightLowerLeg, HumanBodyBones.RightFoot, 0.075f, mPa, 1f, 1f, 0.75f);
        // botas: tobillo y pie
        Limb(c, "BotaI", HumanBodyBones.LeftLowerLeg, HumanBodyBones.LeftFoot, 0.085f, mBo, 1f, 1f, 0.3f, 0.35f);
        Limb(c, "BotaD", HumanBodyBones.RightLowerLeg, HumanBodyBones.RightFoot, 0.085f, mBo, 1f, 1f, 0.3f, 0.35f);
        Limb(c, "PieI", HumanBodyBones.LeftFoot, HumanBodyBones.LeftToes, 0.075f, mBo, 1f, 1.1f, 1.25f);
        Limb(c, "PieD", HumanBodyBones.RightFoot, HumanBodyBones.RightToes, 0.075f, mBo, 1f, 1.1f, 1.25f);
        // mangas y munequeras
        Limb(c, "MangaI", HumanBodyBones.LeftUpperArm, HumanBodyBones.LeftLowerArm, 0.07f, role == Role.Hobbit ? mSl : mTop);
        Limb(c, "MangaD", HumanBodyBones.RightUpperArm, HumanBodyBones.RightLowerArm, 0.07f, role == Role.Hobbit ? mSl : mTop);
        Limb(c, "AntebrazoI", HumanBodyBones.LeftLowerArm, HumanBodyBones.LeftHand, 0.058f, mSl, 1f, 1f, 0.8f);
        Limb(c, "AntebrazoD", HumanBodyBones.RightLowerArm, HumanBodyBones.RightHand, 0.058f, mSl, 1f, 1f, 0.8f);
        Limb(c, "PunoI", HumanBodyBones.LeftLowerArm, HumanBodyBones.LeftHand, 0.066f, mTr, 1f, 1f, 0.22f, 0.4f);
        Limb(c, "PunoD", HumanBodyBones.RightLowerArm, HumanBodyBones.RightHand, 0.066f, mTr, 1f, 1f, 0.22f, 0.4f);
        // dobladillo del pantalon / de la tunica
        Limb(c, "BajoI", HumanBodyBones.LeftLowerLeg, HumanBodyBones.LeftFoot, 0.08f, mTr, 1f, 1f, 0.12f, 0.15f);
        Limb(c, "BajoD", HumanBodyBones.RightLowerLeg, HumanBodyBones.RightFoot, 0.08f, mTr, 1f, 1f, 0.12f, 0.15f);
        if (role == Role.Mage)
            Add(c, "Faldon", Hb, PrimitiveType.Cylinder, new Vector3(0f, -0.3f, 0f), new Vector3(0.44f, 0.3f, 0.36f), Vector3.zero, mTop);
    }

    public static void Apply(Transform root, Animator an, Role role)
    {
        if (an == null || !an.isHuman) return;
        if (root.Find("Disenio") != null) return;
        var c = new Ctx { root = root, an = an, s = Mathf.Max(0.3f, root.lossyScale.y) };
        c.holder = new GameObject("Disenio").transform;
        c.holder.SetParent(root, false);

        var leather = c.Mat(new Color(0.35f, 0.22f, 0.12f));
        var gold = c.Mat(new Color(0.9f, 0.7f, 0.2f), 0.8f, 0.6f);
        var steel = c.Mat(new Color(0.55f, 0.58f, 0.62f), 0.9f, 0.6f);
        var H = HumanBodyBones.Head; var Hi = HumanBodyBones.Hips;
        var chest = an.GetBoneTransform(HumanBodyBones.UpperChest) != null ? HumanBodyBones.UpperChest : HumanBodyBones.Chest;

        switch (role)
        {
            case Role.Hobbit:
            {
                var green = c.Mat(new Color(0.25f, 0.5f, 0.2f));
                Add(c, "Gorro", H, PrimitiveType.Sphere, new Vector3(0f, 0.15f, -0.01f), new Vector3(0.3f, 0.2f, 0.32f), Vector3.zero, green, true);
                Add(c, "Pluma", H, PrimitiveType.Cube, new Vector3(0.08f, 0.27f, -0.02f), new Vector3(0.02f, 0.2f, 0.05f), new Vector3(0f, 0f, -25f), c.Mat(new Color(0.9f, 0.3f, 0.2f)), true);
                Ring(c, "Bufanda", H, new Vector3(0f, -0.1f, 0f), 0.095f, 0.095f, 10, new Vector3(0.07f, 0.07f, 0.06f), c.Mat(new Color(0.85f, 0.7f, 0.2f)));
                Add(c, "Mochila", chest, PrimitiveType.Cube, new Vector3(0f, -0.02f, -0.2f), new Vector3(0.32f, 0.38f, 0.16f), Vector3.zero, leather);
                Add(c, "Petate", chest, PrimitiveType.Cylinder, new Vector3(0f, 0.22f, -0.2f), new Vector3(0.13f, 0.17f, 0.13f), new Vector3(0f, 0f, 90f), c.Mat(new Color(0.55f, 0.35f, 0.25f)));
                Ring(c, "Cinturon", Hi, new Vector3(0f, 0.09f, 0f), 0.2f, 0.14f, 14, new Vector3(0.09f, 0.05f, 0.05f), leather);
                Add(c, "Hebilla", Hi, PrimitiveType.Cube, new Vector3(0f, 0.09f, 0.15f), new Vector3(0.07f, 0.06f, 0.03f), Vector3.zero, gold);
                break;
            }
            case Role.Mage:
            {
                var purple = c.Mat(new Color(0.35f, 0.15f, 0.6f));
                var cape = c.Mat(new Color(0.22f, 0.1f, 0.4f));
                Add(c, "Ala", H, PrimitiveType.Cylinder, new Vector3(0f, 0.14f, 0f), new Vector3(0.5f, 0.012f, 0.5f), Vector3.zero, purple, true);
                Add(c, "Cono1", H, PrimitiveType.Cylinder, new Vector3(0f, 0.2f, 0f), new Vector3(0.24f, 0.06f, 0.24f), Vector3.zero, purple, true);
                Add(c, "Cono2", H, PrimitiveType.Cylinder, new Vector3(0f, 0.32f, 0f), new Vector3(0.18f, 0.07f, 0.18f), Vector3.zero, purple, true);
                Add(c, "Cono3", H, PrimitiveType.Cylinder, new Vector3(0f, 0.43f, -0.01f), new Vector3(0.12f, 0.06f, 0.12f), Vector3.zero, purple, true);
                Add(c, "Cono4", H, PrimitiveType.Cylinder, new Vector3(0f, 0.52f, -0.03f), new Vector3(0.06f, 0.05f, 0.06f), Vector3.zero, purple, true);
                Add(c, "Estrella", H, PrimitiveType.Sphere, new Vector3(0f, 0.19f, 0.115f), Vector3.one * 0.05f, Vector3.zero, c.Mat(new Color(1f, 0.85f, 0.2f), 0f, 0.5f, 1.5f), true);
                Add(c, "Capa", chest, PrimitiveType.Cube, new Vector3(0f, -0.35f, -0.15f), new Vector3(0.42f, 0.8f, 0.03f), new Vector3(6f, 0f, 0f), cape);
                Add(c, "Cuello", chest, PrimitiveType.Cube, new Vector3(0f, 0.17f, -0.1f), new Vector3(0.36f, 0.12f, 0.1f), Vector3.zero, cape);
                Ring(c, "Cinturon", Hi, new Vector3(0f, 0.09f, 0f), 0.2f, 0.14f, 14, new Vector3(0.09f, 0.05f, 0.05f), c.Mat(new Color(0.3f, 0.2f, 0.45f)));
                Add(c, "Hebilla", Hi, PrimitiveType.Sphere, new Vector3(0f, 0.09f, 0.15f), Vector3.one * 0.06f, Vector3.zero, gold);
                var staff = Add(c, "Baston", HumanBodyBones.RightHand, PrimitiveType.Cylinder, new Vector3(0.02f, 0.1f, 0.05f), new Vector3(0.035f, 0.85f, 0.035f), Vector3.zero, c.Mat(new Color(0.4f, 0.26f, 0.14f)));
                var orb = Add(c, "Orbe", HumanBodyBones.RightHand, PrimitiveType.Sphere, new Vector3(0.02f, 1.0f, 0.05f), Vector3.one * 0.14f, Vector3.zero, c.Mat(new Color(0.6f, 0.4f, 1f), 0f, 0.9f, 2.2f));
                if (orb != null) { var l = orb.AddComponent<Light>(); l.type = LightType.Point; l.range = 6f; l.intensity = 2.5f; l.color = new Color(0.6f, 0.45f, 1f); }
                break;
            }
            case Role.Engineer:
            {
                var strap = c.Mat(new Color(0.3f, 0.2f, 0.12f));
                var glass = c.Mat(new Color(0.3f, 0.9f, 1f), 0f, 0.9f, 1.5f);
                var orange = c.Mat(new Color(0.9f, 0.45f, 0.1f), 0.5f, 0.5f);
                Ring(c, "Diadema", H, new Vector3(0f, 0.09f, 0f), 0.115f, 0.115f, 12, new Vector3(0.06f, 0.04f, 0.04f), strap, true);
                Add(c, "LenteI", H, PrimitiveType.Cylinder, new Vector3(-0.05f, 0.11f, 0.11f), new Vector3(0.07f, 0.02f, 0.07f), new Vector3(90f, 0f, 0f), glass, true);
                Add(c, "LenteD", H, PrimitiveType.Cylinder, new Vector3(0.05f, 0.11f, 0.11f), new Vector3(0.07f, 0.02f, 0.07f), new Vector3(90f, 0f, 0f), glass, true);
                Add(c, "MarcoI", H, PrimitiveType.Cylinder, new Vector3(-0.05f, 0.11f, 0.105f), new Vector3(0.09f, 0.015f, 0.09f), new Vector3(90f, 0f, 0f), gold, true);
                Add(c, "MarcoD", H, PrimitiveType.Cylinder, new Vector3(0.05f, 0.11f, 0.105f), new Vector3(0.09f, 0.015f, 0.09f), new Vector3(90f, 0f, 0f), gold, true);
                Add(c, "Mochila", chest, PrimitiveType.Cube, new Vector3(0f, 0f, -0.2f), new Vector3(0.34f, 0.4f, 0.16f), Vector3.zero, c.Mat(new Color(0.3f, 0.32f, 0.36f), 0.7f, 0.5f));
                Add(c, "TanqueI", chest, PrimitiveType.Cylinder, new Vector3(-0.1f, 0.0f, -0.32f), new Vector3(0.12f, 0.2f, 0.12f), Vector3.zero, orange);
                Add(c, "TanqueD", chest, PrimitiveType.Cylinder, new Vector3(0.1f, 0.0f, -0.32f), new Vector3(0.12f, 0.2f, 0.12f), Vector3.zero, orange);
                Add(c, "Engranaje", chest, PrimitiveType.Cylinder, new Vector3(0f, 0.12f, -0.3f), new Vector3(0.16f, 0.015f, 0.16f), new Vector3(90f, 0f, 0f), gold);
                Ring(c, "Cinturon", Hi, new Vector3(0f, 0.09f, 0f), 0.2f, 0.14f, 14, new Vector3(0.09f, 0.06f, 0.05f), strap);
                Add(c, "Llave", Hi, PrimitiveType.Cube, new Vector3(0.2f, -0.05f, 0.05f), new Vector3(0.04f, 0.3f, 0.04f), new Vector3(0f, 0f, 8f), steel);
                Add(c, "Bolsa", Hi, PrimitiveType.Cube, new Vector3(-0.2f, -0.03f, 0.06f), new Vector3(0.1f, 0.14f, 0.1f), Vector3.zero, leather);
                break;
            }
            default: // Peleador
            {
                var red = c.Mat(new Color(0.75f, 0.12f, 0.12f));
                Ring(c, "Banda", H, new Vector3(0f, 0.08f, 0f), 0.115f, 0.12f, 12, new Vector3(0.06f, 0.05f, 0.04f), steel, true);
                Add(c, "Cresta", H, PrimitiveType.Cube, new Vector3(0f, 0.2f, -0.02f), new Vector3(0.04f, 0.1f, 0.3f), Vector3.zero, red, true);
                Add(c, "CascoTapa", H, PrimitiveType.Sphere, new Vector3(0f, 0.12f, -0.01f), new Vector3(0.25f, 0.14f, 0.27f), Vector3.zero, steel, true);
                Add(c, "HombreraI", HumanBodyBones.LeftShoulder, PrimitiveType.Sphere, new Vector3(-0.04f, 0.05f, 0f), new Vector3(0.22f, 0.12f, 0.22f), Vector3.zero, steel);
                Add(c, "HombreraD", HumanBodyBones.RightShoulder, PrimitiveType.Sphere, new Vector3(0.04f, 0.05f, 0f), new Vector3(0.22f, 0.12f, 0.22f), Vector3.zero, steel);
                Add(c, "Pechera", chest, PrimitiveType.Cube, new Vector3(0f, 0f, 0.1f), new Vector3(0.34f, 0.32f, 0.05f), Vector3.zero, steel);
                Add(c, "Capa", chest, PrimitiveType.Cube, new Vector3(0f, -0.25f, -0.15f), new Vector3(0.4f, 0.6f, 0.03f), new Vector3(5f, 0f, 0f), red);
                Ring(c, "Cinturon", Hi, new Vector3(0f, 0.09f, 0f), 0.2f, 0.14f, 14, new Vector3(0.1f, 0.07f, 0.05f), leather);
                Add(c, "Hebilla", Hi, PrimitiveType.Cube, new Vector3(0f, 0.09f, 0.15f), new Vector3(0.1f, 0.09f, 0.03f), Vector3.zero, gold);
                break;
            }
        }
    }
}

// Pegado a un hueso con la orientacion del personaje (no la del hueso, que varia segun el modelo)
public class BoneFollow : MonoBehaviour
{
    public Transform bone, root;
    public Vector3 offset;
    public Quaternion rot = Quaternion.identity;

    void LateUpdate()
    {
        if (bone == null || root == null) return;
        transform.position = bone.position + root.rotation * offset;
        transform.rotation = root.rotation * rot;
    }
}

// Cilindro que cubre el tramo entre dos huesos (se estira y gira con ellos)
public class LimbFollow : MonoBehaviour
{
    public Transform a, b;
    public float radius = 0.08f, sx = 1f, sz = 1f, over = 1.06f, shift = 0f;   // shift: 0 = centrado, +0.5 = pegado a b

    void LateUpdate()
    {
        if (a == null || b == null) return;
        Vector3 d = b.position - a.position;
        float len = d.magnitude;
        if (len < 0.001f) return;
        Vector3 dir = d / len;
        float use = len * over;
        Vector3 mid = a.position + dir * (len * (0.5f + shift * 0.5f));
        transform.position = mid;
        transform.rotation = Quaternion.FromToRotation(Vector3.up, dir);
        transform.localScale = new Vector3(radius * 2f * sx, use * 0.5f, radius * 2f * sz);
    }
}
