using System.Collections.Generic;
using UnityEngine;

// Modelos procedurales (solo primitivas, sin archivos): proyectil del mago, cristal-objetivo y casita del Hobbit.
public static class PropModels
{
    static Material baseMat;
    static readonly Dictionary<string, Material> cache = new Dictionary<string, Material>();

    public static Material Mat(Color c, float emis = 0f, float smooth = 0.4f, float metal = 0f)
    {
        string key = c.ToString() + emis + smooth + metal;
        if (cache.TryGetValue(key, out var m) && m != null) return m;
        if (baseMat == null)
        {
            var t = GameObject.CreatePrimitive(PrimitiveType.Cube);
            baseMat = t.GetComponent<Renderer>().sharedMaterial;
            Object.Destroy(t);
        }
        m = new Material(baseMat) { color = c };
        if (m.HasProperty("_Smoothness")) m.SetFloat("_Smoothness", smooth);
        if (m.HasProperty("_Metallic")) m.SetFloat("_Metallic", metal);
        if (emis > 0f)
        {
            m.EnableKeyword("_EMISSION");
            if (m.HasProperty("_EmissionColor")) m.SetColor("_EmissionColor", c * emis);
        }
        cache[key] = m;
        return m;
    }

    // pieza decorativa sin colision
    public static GameObject Piece(Transform parent, string name, PrimitiveType t, Vector3 lp, Vector3 scale, Vector3 euler, Material m, bool collider = false)
    {
        var go = GameObject.CreatePrimitive(t);
        go.name = name;
        if (!collider) { var c = go.GetComponent<Collider>(); if (c != null) Object.Destroy(c); }
        go.transform.SetParent(parent, false);
        go.transform.localPosition = lp;
        go.transform.localRotation = Quaternion.Euler(euler);
        go.transform.localScale = scale;
        var r = go.GetComponent<Renderer>();
        r.sharedMaterial = m;
        r.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
        return go;
    }

    // ---------------- proyectil del mago ----------------
    // Cometa de energia: nucleo blanco, cabeza alargada, cola que se apaga, aro de runas girando y destello en cruz.
    public static GameObject BuildBolt(Vector3 pos, Vector3 dir, int power)
    {
        float k = power > 1 ? 1.5f : 1f;
        Color main = power > 1 ? new Color(1f, 0.55f, 0.95f) : new Color(0.3f, 0.85f, 1f);
        Color deep = power > 1 ? new Color(0.6f, 0.2f, 0.9f) : new Color(0.25f, 0.4f, 1f);
        var root = new GameObject("MagicProjectile");
        root.transform.position = pos;
        if (dir.sqrMagnitude > 0.001f) root.transform.forward = dir;
        var sc = root.AddComponent<SphereCollider>();
        sc.radius = 0.28f * k;

        var vis = new GameObject("Modelo").transform;
        vis.SetParent(root.transform, false);
        vis.localScale = Vector3.one * k;

        Piece(vis, "Nucleo", PrimitiveType.Sphere, Vector3.zero, Vector3.one * 0.2f, Vector3.zero, Mat(Color.white, 3f, 0.9f));
        Piece(vis, "Cabeza", PrimitiveType.Sphere, new Vector3(0f, 0f, 0.05f), new Vector3(0.3f, 0.3f, 0.5f), Vector3.zero, Mat(main, 2.2f, 0.9f));
        Piece(vis, "Punta", PrimitiveType.Sphere, new Vector3(0f, 0f, 0.28f), new Vector3(0.14f, 0.14f, 0.26f), Vector3.zero, Mat(Color.Lerp(main, Color.white, 0.6f), 2.6f, 0.9f));
        for (int i = 1; i <= 4; i++)
        {
            float f = 1f - i * 0.2f;
            Piece(vis, "Cola" + i, PrimitiveType.Sphere, new Vector3(0f, 0f, -0.18f * i - 0.1f), new Vector3(0.26f * f, 0.26f * f, 0.3f * f), Vector3.zero, Mat(Color.Lerp(deep, main, f), 1.6f * f + 0.3f, 0.8f));
        }
        // aro de runas
        var ring = new GameObject("Aro").transform;
        ring.SetParent(vis, false);
        ring.localPosition = new Vector3(0f, 0f, -0.05f);
        for (int i = 0; i < 6; i++)
        {
            float a = i * Mathf.PI * 2f / 6f;
            Piece(ring, "Runa", PrimitiveType.Cube, new Vector3(Mathf.Cos(a) * 0.26f, Mathf.Sin(a) * 0.26f, 0f), new Vector3(0.07f, 0.07f, 0.07f), new Vector3(0f, 0f, a * Mathf.Rad2Deg + 45f), Mat(Color.Lerp(main, Color.white, 0.4f), 2.5f));
        }
        ring.gameObject.AddComponent<MagicSpin>().axis = Vector3.forward;
        ring.GetComponent<MagicSpin>().speed = 360f;
        // destello en cruz
        var star = new GameObject("Destello").transform;
        star.SetParent(vis, false);
        Piece(star, "BrazoA", PrimitiveType.Cube, Vector3.zero, new Vector3(0.7f, 0.04f, 0.04f), Vector3.zero, Mat(Color.white, 2.5f));
        Piece(star, "BrazoB", PrimitiveType.Cube, Vector3.zero, new Vector3(0.04f, 0.7f, 0.04f), Vector3.zero, Mat(Color.white, 2.5f));
        star.gameObject.AddComponent<MagicSpin>().axis = Vector3.forward;
        star.GetComponent<MagicSpin>().speed = -220f;

        var lg = new GameObject("Luz");
        lg.transform.SetParent(root.transform, false);
        var l = lg.AddComponent<Light>();
        l.type = LightType.Point; l.range = 6f * k; l.intensity = 3f; l.color = main;
        return root;
    }

    // estallido de chispas al impactar
    public static void Burst(Vector3 pos, Color col, int n = 10)
    {
        var m = Mat(col, 2.5f);
        for (int i = 0; i < n; i++)
        {
            var s = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            Object.Destroy(s.GetComponent<Collider>());
            s.name = "Chispa";
            s.transform.position = pos;
            s.transform.localScale = Vector3.one * Random.Range(0.06f, 0.14f);
            s.GetComponent<Renderer>().sharedMaterial = m;
            s.GetComponent<Renderer>().shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            var b = s.AddComponent<BurstSpark>();
            b.vel = Random.onUnitSphere * Random.Range(2f, 5f);
        }
    }

    // ---------------- cristal-objetivo del mago ----------------
    // Reemplaza el cubo gris por un cristal facetado sobre un altar con runas y motas flotando.
    public static void DressCrystal(Transform parent, GameObject pedestalBox, GameObject crystalBox, Crystal crystal)
    {
        if (pedestalBox != null) pedestalBox.GetComponent<Renderer>().enabled = false;
        if (crystalBox != null) crystalBox.GetComponent<Renderer>().enabled = false;
        Vector3 floor = new Vector3(crystalBox.transform.localPosition.x, pedestalBox != null ? pedestalBox.transform.localPosition.y - 0.6f : crystalBox.transform.localPosition.y - 2f, crystalBox.transform.localPosition.z);

        var root = new GameObject("CristalModelo").transform;
        root.SetParent(parent, false);
        root.localPosition = floor;

        var stone = Mat(new Color(0.36f, 0.35f, 0.4f), 0f, 0.25f);
        var dark = Mat(new Color(0.22f, 0.22f, 0.26f), 0f, 0.2f);
        var gold = Mat(new Color(0.95f, 0.75f, 0.25f), 0.3f, 0.7f, 0.8f);
        var rune = Mat(new Color(0.35f, 0.9f, 1f), 1.6f);

        // altar escalonado
        Piece(root, "Base1", PrimitiveType.Cube, new Vector3(0f, 0.15f, 0f), new Vector3(1.7f, 0.3f, 1.7f), Vector3.zero, dark);
        Piece(root, "Base2", PrimitiveType.Cube, new Vector3(0f, 0.45f, 0f), new Vector3(1.3f, 0.3f, 1.3f), new Vector3(0f, 45f, 0f), stone);
        Piece(root, "Columna", PrimitiveType.Cube, new Vector3(0f, 0.95f, 0f), new Vector3(0.75f, 0.7f, 0.75f), Vector3.zero, stone);
        Piece(root, "AroOro", PrimitiveType.Cylinder, new Vector3(0f, 0.62f, 0f), new Vector3(0.95f, 0.04f, 0.95f), Vector3.zero, gold);
        Piece(root, "AroOro2", PrimitiveType.Cylinder, new Vector3(0f, 1.32f, 0f), new Vector3(0.95f, 0.04f, 0.95f), Vector3.zero, gold);
        for (int i = 0; i < 4; i++)
        {
            float a = i * 90f * Mathf.Deg2Rad;
            Piece(root, "RunaAltar", PrimitiveType.Cube, new Vector3(Mathf.Sin(a) * 0.385f, 0.95f, Mathf.Cos(a) * 0.385f), new Vector3(0.08f, 0.38f, 0.08f), new Vector3(0f, i * 90f, 0f), rune);
        }

        // cristal: cuerpo + puntas + esquirlas
        var cr = new GameObject("Cristal").transform;
        cr.SetParent(root, false);
        cr.localPosition = new Vector3(0f, 2.0f, 0f);
        var cm = Mat(new Color(0.35f, 0.62f, 0.95f), 0.5f, 0.95f);
        var rends = new List<Renderer>();
        rends.Add(Piece(cr, "Cuerpo", PrimitiveType.Cube, Vector3.zero, new Vector3(0.5f, 1.3f, 0.5f), new Vector3(0f, 45f, 0f), cm).GetComponent<Renderer>());
        rends.Add(Piece(cr, "PuntaArriba", PrimitiveType.Cube, new Vector3(0f, 0.8f, 0f), new Vector3(0.5f, 0.5f, 0.5f), new Vector3(45f, 0f, 45f), cm).GetComponent<Renderer>());
        rends.Add(Piece(cr, "PuntaAbajo", PrimitiveType.Cube, new Vector3(0f, -0.8f, 0f), new Vector3(0.5f, 0.5f, 0.5f), new Vector3(45f, 0f, 45f), cm).GetComponent<Renderer>());
        float[] ang = { 20f, 110f, 200f, 290f };
        for (int i = 0; i < 4; i++)
        {
            float a = ang[i] * Mathf.Deg2Rad;
            var sh = Piece(cr, "Esquirla", PrimitiveType.Cube, new Vector3(Mathf.Sin(a) * 0.42f, -0.35f + (i % 2) * 0.3f, Mathf.Cos(a) * 0.42f), new Vector3(0.2f, 0.75f, 0.2f), new Vector3(Mathf.Cos(a) * 22f, ang[i], -Mathf.Sin(a) * 22f), cm);
            rends.Add(sh.GetComponent<Renderer>());
        }
        cr.gameObject.AddComponent<MagicSpin>().axis = Vector3.up;
        cr.GetComponent<MagicSpin>().speed = 25f;

        // motas orbitando
        var orb = new GameObject("Motas").transform;
        orb.SetParent(root, false);
        orb.localPosition = new Vector3(0f, 2.0f, 0f);
        for (int i = 0; i < 6; i++)
        {
            float a = i * Mathf.PI * 2f / 6f;
            Piece(orb, "Mota", PrimitiveType.Cube, new Vector3(Mathf.Cos(a) * 0.95f, Mathf.Sin(i * 1.7f) * 0.5f, Mathf.Sin(a) * 0.95f), Vector3.one * 0.1f, new Vector3(45f, 45f, 0f), rune);
        }
        orb.gameObject.AddComponent<MagicSpin>().axis = Vector3.up;
        orb.GetComponent<MagicSpin>().speed = -55f;

        var look = root.gameObject.AddComponent<CrystalLook>();
        look.crystal = crystal; look.rends = rends.ToArray(); look.cap = cr;
    }

    // ---------------- casita del Hobbit ----------------
    // Techo a dos aguas con tejas, frontones de piedra, chimenea, antorchas y runas junto a la gatera.
    public static void DressHut(Transform c, float cx, float cz, float s)
    {
        var roof = Mat(new Color(0.55f, 0.2f, 0.14f), 0f, 0.15f);
        var roofDark = Mat(new Color(0.35f, 0.12f, 0.1f), 0f, 0.15f);
        var stone = Mat(new Color(0.42f, 0.4f, 0.44f), 0f, 0.2f);
        var wood = Mat(new Color(0.4f, 0.26f, 0.14f), 0f, 0.2f);
        var gold = Mat(new Color(0.95f, 0.75f, 0.25f), 0.4f, 0.7f, 0.8f);
        var rune = Mat(new Color(0.45f, 1f, 0.55f), 1.6f);
        var flame = Mat(new Color(1f, 0.55f, 0.1f), 3f);
        var root = new GameObject("CasitaDeco").transform;
        root.SetParent(c, false);
        root.localPosition = new Vector3(cx, 0f, cz);

        float zf = -s * 3.5f, zb = s * 3.5f;      // pared de frente (hacia el centro del piso) y pared del fondo, en local
        float o = -s;                              // sentido "hacia afuera" por el frente
        float len = 8.6f;
        // dos aguas (cada lado sube hacia el centro)
        Piece(root, "TejadoI", PrimitiveType.Cube, new Vector3(-1.8f, 5.5f, 0f), new Vector3(4.9f, 0.35f, len), new Vector3(0f, 0f, 30f), roof);
        Piece(root, "TejadoD", PrimitiveType.Cube, new Vector3(1.8f, 5.5f, 0f), new Vector3(4.9f, 0.35f, len), new Vector3(0f, 0f, -30f), roof);
        Piece(root, "Cumbrera", PrimitiveType.Cube, new Vector3(0f, 6.55f, 0f), new Vector3(0.5f, 0.3f, len + 0.2f), new Vector3(0f, 0f, 45f), roofDark);
        for (int i = 0; i < 5; i++)
        {
            float z = -len * 0.5f + 0.5f + i * (len - 1f) / 4f;
            Piece(root, "TejaI", PrimitiveType.Cube, new Vector3(-1.8f, 5.73f, z), new Vector3(4.9f, 0.1f, 0.18f), new Vector3(0f, 0f, 30f), roofDark);
            Piece(root, "TejaD", PrimitiveType.Cube, new Vector3(1.8f, 5.73f, z), new Vector3(4.9f, 0.1f, 0.18f), new Vector3(0f, 0f, -30f), roofDark);
        }
        // frontones escalonados delante y detras
        foreach (float zz in new[] { zf, zb })
        {
            Piece(root, "Fronton1", PrimitiveType.Cube, new Vector3(0f, 4.7f, zz), new Vector3(5.2f, 0.4f, 1f), Vector3.zero, stone, true);
            Piece(root, "Fronton2", PrimitiveType.Cube, new Vector3(0f, 5.1f, zz), new Vector3(3.8f, 0.4f, 1f), Vector3.zero, stone, true);
            Piece(root, "Fronton3", PrimitiveType.Cube, new Vector3(0f, 5.5f, zz), new Vector3(2.4f, 0.4f, 1f), Vector3.zero, stone, true);
            Piece(root, "Fronton4", PrimitiveType.Cube, new Vector3(0f, 5.9f, zz), new Vector3(1.0f, 0.4f, 1f), Vector3.zero, stone, true);
        }
        // chimenea
        Piece(root, "Chimenea", PrimitiveType.Cube, new Vector3(1.8f, 6.6f, zb - o * -1f * 0f - s * 2f), new Vector3(0.9f, 2.2f, 0.9f), Vector3.zero, stone);
        Piece(root, "ChimeneaTapa", PrimitiveType.Cube, new Vector3(1.8f, 7.75f, zb - s * 2f), new Vector3(1.15f, 0.2f, 1.15f), Vector3.zero, roofDark);
        // letrero de madera sobre la gatera
        float zo = zf + o * 0.65f;                 // justo delante de la pared de frente
        Piece(root, "Letrero", PrimitiveType.Cube, new Vector3(0f, 3.75f, zo), new Vector3(2.4f, 0.55f, 0.12f), Vector3.zero, wood);
        Piece(root, "LetreroOro", PrimitiveType.Cube, new Vector3(0f, 3.75f, zo + o * 0.07f), new Vector3(0.5f, 0.3f, 0.05f), Vector3.zero, gold);
        // antorchas y runas a los lados de la entrada
        foreach (float sx in new[] { -1f, 1f })
        {
            Piece(root, "SoporteAntorcha", PrimitiveType.Cube, new Vector3(sx * 1.6f, 2.0f, zo), new Vector3(0.12f, 0.5f, 0.12f), Vector3.zero, wood);
            var fl = Piece(root, "Llama", PrimitiveType.Sphere, new Vector3(sx * 1.6f, 2.4f, zo), new Vector3(0.25f, 0.35f, 0.25f), Vector3.zero, flame);
            var lg = new GameObject("LuzAntorcha"); lg.transform.SetParent(fl.transform, false);
            var l = lg.AddComponent<Light>(); l.type = LightType.Point; l.range = 7f; l.intensity = 4f; l.color = new Color(1f, 0.6f, 0.25f);
            for (int i = 0; i < 3; i++)
                Piece(root, "Runa", PrimitiveType.Cube, new Vector3(sx * 2.6f, 1.2f + i * 0.9f, zf + o * 0.55f), new Vector3(0.28f, 0.28f, 0.06f), new Vector3(0f, 0f, 45f), rune);
        }
        // marco dorado de la gatera
        Piece(root, "MarcoGatera", PrimitiveType.Cube, new Vector3(0f, 1.25f, zf + o * 0.52f), new Vector3(2.15f, 0.1f, 0.08f), Vector3.zero, gold);
    }
}

public class MagicSpin : MonoBehaviour
{
    public Vector3 axis = Vector3.up;
    public float speed = 90f;
    void Update() { transform.Rotate(axis, speed * Time.deltaTime, Space.Self); }
}

public class BurstSpark : MonoBehaviour
{
    public Vector3 vel;
    float t;
    void Update()
    {
        t += Time.deltaTime;
        transform.position += vel * Time.deltaTime;
        vel += Vector3.down * 6f * Time.deltaTime;
        float k = Mathf.Clamp01(1f - t / 0.55f);
        transform.localScale = Vector3.one * 0.12f * k;
        if (t > 0.55f) Destroy(gameObject);
    }
}

// Cristal: brillo suave en reposo; al recibir el disparo se enciende fuerte y suelta chispas.
public class CrystalLook : MonoBehaviour
{
    public Crystal crystal;
    public Renderer[] rends;
    public Transform cap;
    bool lit;
    Material live, hitMat;

    void Start()
    {
        live = new Material(PropModels.Mat(new Color(0.35f, 0.62f, 0.95f), 0.5f, 0.95f));
        hitMat = new Material(PropModels.Mat(new Color(0.4f, 1f, 1f), 3.2f, 0.95f));
        foreach (var r in rends) if (r != null) r.sharedMaterial = live;
    }

    void Update()
    {
        if (cap != null) cap.localPosition = new Vector3(0f, 2.0f + Mathf.Sin(Time.time * 1.6f) * 0.08f, 0f);
        if (!lit)
        {
            float p = 0.5f + 0.3f * Mathf.Sin(Time.time * 2.2f);
            if (live != null && live.HasProperty("_EmissionColor")) live.SetColor("_EmissionColor", new Color(0.35f, 0.62f, 0.95f) * p);
            if (crystal != null && crystal.IsHit)
            {
                lit = true;
                foreach (var r in rends) if (r != null) r.sharedMaterial = hitMat;
                PropModels.Burst(transform.position + Vector3.up * 2f, new Color(0.5f, 1f, 1f), 18);
            }
        }
    }
}

// Texturas procedurales (sin archivos) para plataformas, puente, torre, placas y puertas.
public static class Surf
{
    public enum Kind { Stone, Wood, Door, Plate }
    static readonly Dictionary<Kind, Material> mats = new Dictionary<Kind, Material>();

    static float H(int x, int y, int seed)
    {
        unchecked
        {
            int h = x * 73856093 ^ y * 19349663 ^ seed * 83492791;
            h = (h ^ (h >> 13)) * 1274126177;
            return ((h ^ (h >> 16)) & 0xffff) / 65535f;
        }
    }

    static Texture2D MakeTex(Kind k)
    {
        const int N = 128;
        var t = new Texture2D(N, N, TextureFormat.RGBA32, true) { wrapMode = TextureWrapMode.Repeat, filterMode = FilterMode.Bilinear };
        var px = new Color[N * N];
        for (int y = 0; y < N; y++)
            for (int x = 0; x < N; x++)
            {
                Color c;
                switch (k)
                {
                    case Kind.Stone:
                    {
                        int row = y / 32, off = (row % 2) * 32;
                        int bx = (x + off) % 64, by = y % 32;
                        int brickId = row * 7 + (x + off) / 64;
                        bool mortar = bx < 3 || by < 3;
                        float v = 0.55f + (H(brickId, 3, 1) - 0.5f) * 0.25f + (H(x, y, 5) - 0.5f) * 0.08f;
                        c = mortar ? new Color(0.25f, 0.25f, 0.28f) : new Color(v, v, v * 1.07f);
                        break;
                    }
                    case Kind.Wood:
                    {
                        int plank = x / 32, px2 = x % 32;
                        bool seam = px2 < 2;
                        float grain = Mathf.Sin((y + H(plank, 0, 2) * 90f) * 0.18f + Mathf.Sin(x * 0.4f) * 1.2f) * 0.5f + 0.5f;
                        float v = 0.42f + (H(plank, 1, 3) - 0.5f) * 0.18f + grain * 0.12f;
                        c = seam ? new Color(0.14f, 0.09f, 0.05f) : new Color(v * 1.15f, v * 0.78f, v * 0.45f);
                        break;
                    }
                    case Kind.Door:
                    {
                        int plank = x / 32, px2 = x % 32;
                        bool seam = px2 < 2;
                        float grain = Mathf.Sin((y + H(plank, 0, 2) * 90f) * 0.2f + Mathf.Sin(x * 0.4f) * 1.1f) * 0.5f + 0.5f;
                        float v = 0.38f + (H(plank, 1, 3) - 0.5f) * 0.16f + grain * 0.1f;
                        c = seam ? new Color(0.1f, 0.06f, 0.04f) : new Color(v * 1.15f, v * 0.78f, v * 0.45f);
                        bool band = (y > 14 && y < 30) || (y > 98 && y < 114);
                        if (band)
                        {
                            float m = 0.28f + (H(x, y, 9) - 0.5f) * 0.05f;
                            c = new Color(m, m, m * 1.1f);
                            int rx = x % 32, ry = y % 32;
                            if ((rx - 16) * (rx - 16) + (ry - 22) * (ry - 22) < 9) c = new Color(0.7f, 0.6f, 0.3f);
                        }
                        break;
                    }
                    default: // Plate
                    {
                        bool edge = x < 6 || y < 6 || x >= N - 6 || y >= N - 6;
                        float m = 0.5f + (H(x / 2, y / 2, 4) - 0.5f) * 0.08f;
                        c = edge ? new Color(0.3f, 0.3f, 0.34f) : new Color(m, m, m * 1.05f);
                        foreach (var q in new[] { new Vector2(12, 12), new Vector2(N - 12, 12), new Vector2(12, N - 12), new Vector2(N - 12, N - 12) })
                            if ((x - q.x) * (x - q.x) + (y - q.y) * (y - q.y) < 10) c = new Color(0.75f, 0.7f, 0.5f);
                        break;
                    }
                }
                px[y * N + x] = c;
            }
        t.SetPixels(px);
        t.Apply(true);
        return t;
    }

    static Material Get(Kind k)
    {
        if (mats.TryGetValue(k, out var m) && m != null) return m;
        var t = GameObject.CreatePrimitive(PrimitiveType.Cube);
        m = new Material(t.GetComponent<Renderer>().sharedMaterial);
        Object.Destroy(t);
        var tex = MakeTex(k);
        m.mainTexture = tex;
        if (m.HasProperty("_BaseMap")) m.SetTexture("_BaseMap", tex);
        m.color = Color.white;
        if (m.HasProperty("_BaseColor")) m.SetColor("_BaseColor", Color.white);
        if (m.HasProperty("_Smoothness")) m.SetFloat("_Smoothness", k == Kind.Plate ? 0.5f : 0.1f);
        mats[k] = m;
        return m;
    }

    // revista un cubo con la textura (UV en metros: el patron no se estira)
    public static GameObject Apply(GameObject go, Kind kind, float tile = 3f)
    {
        if (go == null) return go;
        var mf = go.GetComponent<MeshFilter>();
        var mr = go.GetComponent<Renderer>();
        if (mf == null || mr == null) return go;
        mf.sharedMesh = StoneSkin.BoxMesh(go.transform.localScale, tile);
        mr.sharedMaterial = Get(kind);
        return go;
    }
}
