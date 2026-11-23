using UnityEngine;

// Aspecto del Cronarca (senor del tiempo), armado por codigo: reloj-tambor dorado con dos caratulas,
// manecillas, anillos orbitales, engranajes flotantes y dos relojes de arena que giran y se voltean.
// tiempo para animaciones decorativas: puede ignorar la pausa (cinematicas con timeScale = 0)
public static class CosmosTime
{
    public static bool Ignore;
    public static float Dt => Ignore ? Time.unscaledDeltaTime : Time.deltaTime;
}

public static class CronarcaLook
{
    public static float Speed = 1f;   // sube con las fases del jefe

    static Material baseMat;

    static Material M(Color c, float metal = 0.6f, float smooth = 0.6f, float emis = 0f)
    {
        if (baseMat == null)
        {
            var t = GameObject.CreatePrimitive(PrimitiveType.Cube);
            baseMat = t.GetComponent<Renderer>().sharedMaterial;
            Object.Destroy(t);
        }
        var m = new Material(baseMat);
        m.color = c;
        if (m.HasProperty("_Metallic")) m.SetFloat("_Metallic", metal);
        if (m.HasProperty("_Smoothness")) m.SetFloat("_Smoothness", smooth);
        if (emis > 0f)
        {
            m.EnableKeyword("_EMISSION");
            if (m.HasProperty("_EmissionColor")) m.SetColor("_EmissionColor", c * emis);
        }
        return m;
    }

    static GameObject P(PrimitiveType t, Transform parent, Vector3 pos, Vector3 scale, Material m, string name = "p")
    {
        var go = GameObject.CreatePrimitive(t);
        go.name = name;
        var col = go.GetComponent<Collider>(); if (col != null) Object.Destroy(col);
        go.transform.SetParent(parent, false);
        go.transform.localPosition = pos;
        go.transform.localScale = scale;
        go.GetComponent<Renderer>().sharedMaterial = m;
        return go;
    }

    static Transform Pivot(Transform parent, Vector3 pos, Vector3 euler, string name)
    {
        var go = new GameObject(name);
        go.transform.SetParent(parent, false);
        go.transform.localPosition = pos;
        go.transform.localRotation = Quaternion.Euler(euler);
        return go.transform;
    }

    // devuelve el renderer del cuerpo (el que parpadea al recibir dano)
    public static Renderer Build(Transform root)
    {
        Speed = 1f;
        for (int i = 0; i < root.childCount; i++) root.GetChild(i).gameObject.SetActive(false);
        var look = Pivot(root, Vector3.zero, Vector3.zero, "AspectoCronarca").gameObject.transform;

        var gold = M(new Color(0.85f, 0.62f, 0.18f), 0.9f, 0.7f);
        var goldGlow = M(new Color(1f, 0.75f, 0.25f), 0.3f, 0.6f, 1.6f);
        var dark = M(new Color(0.14f, 0.1f, 0.24f), 0.5f, 0.5f, 0.25f);
        var ivory = M(new Color(0.95f, 0.92f, 0.8f), 0f, 0.3f);
        var ink = M(new Color(0.05f, 0.04f, 0.08f), 0f, 0.2f);
        var sand = M(new Color(0.45f, 0.85f, 1f), 0f, 0.9f, 1.8f);

        // cuerpo: tambor oscuro con borde dorado (eje Z)
        var drumPivot = Pivot(look, Vector3.zero, new Vector3(90f, 0f, 0f), "Tambor");
        var body = P(PrimitiveType.Cylinder, drumPivot, Vector3.zero, new Vector3(3.8f, 0.8f, 3.8f), dark, "Cuerpo");
        P(PrimitiveType.Cylinder, drumPivot, Vector3.zero, new Vector3(4.1f, 0.55f, 4.1f), gold, "Aro");

        // dos caratulas (adelante y atras) con marcas y manecillas
        for (int s = -1; s <= 1; s += 2)
        {
            var face = Pivot(look, new Vector3(0f, 0f, s * 0.84f), Vector3.zero, s < 0 ? "CaraFrente" : "CaraAtras");
            var disk = P(PrimitiveType.Cylinder, face, Vector3.zero, new Vector3(3.3f, 0.03f, 3.3f), ivory, "Esfera");
            disk.transform.localRotation = Quaternion.Euler(90f, 0f, 0f);
            for (int i = 0; i < 12; i++)
            {
                float a = i * 30f * Mathf.Deg2Rad;
                bool big = i % 3 == 0;
                var tk = P(PrimitiveType.Cube, face, new Vector3(Mathf.Sin(a) * 1.4f, Mathf.Cos(a) * 1.4f, s * 0.04f),
                    big ? new Vector3(0.16f, 0.4f, 0.06f) : new Vector3(0.09f, 0.22f, 0.06f), ink, "Marca");
                tk.transform.localRotation = Quaternion.Euler(0f, 0f, -i * 30f);
            }
            var hp = Pivot(face, new Vector3(0f, 0f, s * 0.08f), Vector3.zero, "Hora");
            P(PrimitiveType.Cube, hp, new Vector3(0f, 0.4f, 0f), new Vector3(0.16f, 0.8f, 0.06f), ink, "ManoHora");
            hp.gameObject.AddComponent<TimeSpin>().Setup(TimeSpin.Mode.Spin, Vector3.forward, 6f * -s);
            var mp = Pivot(face, new Vector3(0f, 0f, s * 0.14f), Vector3.zero, "Minuto");
            P(PrimitiveType.Cube, mp, new Vector3(0f, 0.65f, 0f), new Vector3(0.1f, 1.3f, 0.05f), ink, "ManoMinuto");
            mp.gameObject.AddComponent<TimeSpin>().Setup(TimeSpin.Mode.Spin, Vector3.forward, 72f * -s);
            P(PrimitiveType.Sphere, face, new Vector3(0f, 0f, s * 0.16f), Vector3.one * 0.22f, goldGlow, "Centro");
        }

        // anillos orbitales (giroscopio)
        float[] radii = { 3.3f, 3.8f, 4.3f };
        Vector3[] tilts = { new Vector3(0f, 0f, 0f), new Vector3(60f, 0f, 0f), new Vector3(0f, 0f, 60f) };
        float[] spd = { 30f, -22f, 16f };
        for (int r = 0; r < 3; r++)
        {
            var tilt = Pivot(look, Vector3.zero, tilts[r], "AnilloInclinado");
            var ring = Pivot(tilt, Vector3.zero, Vector3.zero, "Anillo");
            ring.gameObject.AddComponent<TimeSpin>().Setup(TimeSpin.Mode.Spin, Vector3.up, spd[r]);
            int n = 22;
            for (int i = 0; i < n; i++)
            {
                float a = i * 360f / n;
                var seg = P(PrimitiveType.Cube, ring, Quaternion.Euler(0f, a, 0f) * new Vector3(0f, 0f, radii[r]),
                    new Vector3(0.28f, 0.14f, 0.7f), i % 5 == 0 ? goldGlow : gold, "Seg");
                seg.transform.localRotation = Quaternion.Euler(0f, a, 0f);
            }
        }

        // engranajes flotantes
        for (int g = 0; g < 4; g++)
        {
            float az = g * 90f + 45f;
            Vector3 pos = Quaternion.Euler(0f, az, 0f) * new Vector3(0f, 1.2f + (g % 2) * 1.6f, 5.2f);
            var gp = Pivot(look, pos, new Vector3(90f, az, 0f), "Engranaje");
            gp.gameObject.AddComponent<TimeSpin>().Setup(TimeSpin.Mode.Spin, Vector3.up, (g % 2 == 0 ? 40f : -40f));
            P(PrimitiveType.Cylinder, gp, Vector3.zero, new Vector3(1.3f, 0.07f, 1.3f), gold, "Disco");
            P(PrimitiveType.Cylinder, gp, new Vector3(0f, 0.05f, 0f), new Vector3(0.4f, 0.09f, 0.4f), dark, "Eje");
            for (int i = 0; i < 9; i++)
            {
                float a = i * 40f;
                var tooth = P(PrimitiveType.Cube, gp, Quaternion.Euler(0f, a, 0f) * new Vector3(0f, 0f, 0.72f),
                    new Vector3(0.24f, 0.08f, 0.26f), gold, "Diente");
                tooth.transform.localRotation = Quaternion.Euler(0f, a, 0f);
            }
        }

        // dos relojes de arena que orbitan y se voltean
        for (int h = 0; h < 2; h++)
        {
            var orbit = Pivot(look, new Vector3(0f, 0.3f, 0f), Vector3.zero, "OrbitaArena");
            orbit.localRotation = Quaternion.Euler(0f, h * 180f, 0f);
            orbit.gameObject.AddComponent<TimeSpin>().Setup(TimeSpin.Mode.Spin, Vector3.up, 25f);
            var hg = Pivot(orbit, new Vector3(0f, 0f, 6.4f), Vector3.zero, "RelojArena");
            hg.localScale = Vector3.one * 1.1f;
            hg.gameObject.AddComponent<TimeSpin>().Setup(TimeSpin.Mode.Flip, Vector3.right, 0f);
            P(PrimitiveType.Cylinder, hg, new Vector3(0f, 1.35f, 0f), new Vector3(1.3f, 0.08f, 1.3f), gold, "TapaArriba");
            P(PrimitiveType.Cylinder, hg, new Vector3(0f, -1.35f, 0f), new Vector3(1.3f, 0.08f, 1.3f), gold, "TapaAbajo");
            P(PrimitiveType.Sphere, hg, new Vector3(0f, 0.7f, 0f), new Vector3(1.05f, 1.2f, 1.05f), sand, "BulboArriba");
            P(PrimitiveType.Sphere, hg, new Vector3(0f, -0.7f, 0f), new Vector3(1.05f, 1.2f, 1.05f), sand, "BulboAbajo");
            P(PrimitiveType.Cylinder, hg, Vector3.zero, new Vector3(0.18f, 0.3f, 0.18f), gold, "Cuello");
            for (int k = 0; k < 3; k++)
            {
                Vector3 o = Quaternion.Euler(0f, k * 120f, 0f) * new Vector3(0.6f, 0f, 0f);
                P(PrimitiveType.Cylinder, hg, o, new Vector3(0.07f, 1.35f, 0.07f), gold, "Poste");
            }
        }

        var lg = new GameObject("LuzCronarca");
        lg.transform.SetParent(look, false);
        var l = lg.AddComponent<Light>();
        l.type = LightType.Point; l.range = 30f; l.intensity = 10f; l.color = new Color(0.6f, 0.85f, 1f);

        return body.GetComponent<Renderer>();
    }
}

// Giro continuo (Spin) o volteo periodico de 180 grados (Flip); se acelera con CronarcaLook.Speed
public class TimeSpin : MonoBehaviour
{
    public enum Mode { Spin, Flip }
    public bool bossSync = true;   // true: se acelera con las fases del Cronarca
    Mode mode; Vector3 axis; float speed; Quaternion baseRot; float t;

    public void Setup(Mode m, Vector3 ax, float sp) { mode = m; axis = ax; speed = sp; baseRot = transform.localRotation; }

    void Update()
    {
        float dt = CosmosTime.Dt * (bossSync ? CronarcaLook.Speed : 1f);
        if (mode == Mode.Spin) { transform.Rotate(axis, speed * dt, Space.Self); return; }
        t += dt;
        float cycle = Mathf.Floor(t / 6f);
        float s = Mathf.SmoothStep(0f, 1f, Mathf.Clamp01(t % 6f));
        transform.localRotation = baseRot * Quaternion.AngleAxis(180f * (cycle + s), axis);
    }
}
