using UnityEngine;

// Aspecto del Ascua: un reloj de arena cosmico y magico (marco dorado, bulbos de nebulosa, arena de estrellas
// que cae, halo giratorio y chispas orbitando). Se arma por codigo sobre el objeto del artefacto.
public class ArtifactVisual : MonoBehaviour
{
    Material baseMat;
    Transform sand;
    float t;

    Material M(Color c, float metal, float smooth, float emis)
    {
        if (baseMat == null)
        {
            var tmp = GameObject.CreatePrimitive(PrimitiveType.Cube);
            baseMat = tmp.GetComponent<Renderer>().sharedMaterial;
            Destroy(tmp);
        }
        var m = new Material(baseMat) { color = c };
        if (m.HasProperty("_Metallic")) m.SetFloat("_Metallic", metal);
        if (m.HasProperty("_Smoothness")) m.SetFloat("_Smoothness", smooth);
        if (emis > 0f) { m.EnableKeyword("_EMISSION"); if (m.HasProperty("_EmissionColor")) m.SetColor("_EmissionColor", c * emis); }
        return m;
    }

    GameObject P(PrimitiveType type, Transform parent, Vector3 pos, Vector3 scl, Material m)
    {
        var g = GameObject.CreatePrimitive(type);
        var col = g.GetComponent<Collider>(); if (col != null) Destroy(col);
        g.transform.SetParent(parent, false);
        g.transform.localPosition = pos;
        g.transform.localScale = scl;
        var r = g.GetComponent<Renderer>();
        r.sharedMaterial = m;
        r.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
        return g;
    }

    void Awake()
    {
        var r0 = GetComponent<Renderer>();
        if (r0 != null) r0.enabled = false;

        var root = new GameObject("RelojArena").transform;
        root.SetParent(transform, false);
        root.localScale = Vector3.one * 1.5f;   // el objeto base mide ~0.35: queda un reloj de ~0.5 m

        var gold = M(new Color(1f, 0.78f, 0.3f), 0.9f, 0.7f, 0.15f);
        var neb1 = M(new Color(0.55f, 0.2f, 1f), 0f, 0.9f, 1.6f);
        var neb2 = M(new Color(0.15f, 0.75f, 1f), 0f, 0.9f, 1.6f);
        var star = M(new Color(1f, 0.98f, 0.85f), 0f, 0.5f, 3f);

        // giro lento y volteo periodico del reloj entero
        var flip = new GameObject("Volteo").transform;
        flip.SetParent(root, false);
        var fs = flip.gameObject.AddComponent<TimeSpin>(); fs.Setup(TimeSpin.Mode.Flip, Vector3.forward, 0f); fs.bossSync = false;
        var spin = new GameObject("Giro").transform;
        spin.SetParent(flip, false);
        var ss = spin.gameObject.AddComponent<TimeSpin>(); ss.Setup(TimeSpin.Mode.Spin, Vector3.up, 35f); ss.bossSync = false;

        // tapas, postes, cuello
        P(PrimitiveType.Cylinder, spin, new Vector3(0f, 0.66f, 0f), new Vector3(0.95f, 0.05f, 0.95f), gold);
        P(PrimitiveType.Cylinder, spin, new Vector3(0f, -0.66f, 0f), new Vector3(0.95f, 0.05f, 0.95f), gold);
        for (int k = 0; k < 3; k++)
        {
            Vector3 o = Quaternion.Euler(0f, k * 120f, 0f) * new Vector3(0.4f, 0f, 0f);
            P(PrimitiveType.Cylinder, spin, o, new Vector3(0.05f, 0.64f, 0.05f), gold);
        }
        P(PrimitiveType.Cylinder, spin, Vector3.zero, new Vector3(0.12f, 0.1f, 0.12f), gold);

        // bulbos de nebulosa
        P(PrimitiveType.Sphere, spin, new Vector3(0f, 0.34f, 0f), new Vector3(0.7f, 0.62f, 0.7f), neb1);
        P(PrimitiveType.Sphere, spin, new Vector3(0f, -0.34f, 0f), new Vector3(0.7f, 0.62f, 0.7f), neb2);
        // estrellitas dentro del bulbo de arriba
        for (int k = 0; k < 6; k++)
        {
            var a = Random.insideUnitSphere * 0.2f;
            P(PrimitiveType.Sphere, spin, new Vector3(a.x, 0.42f + a.y * 0.6f, a.z), Vector3.one * 0.05f, star);
        }
        // arena de estrellas cayendo + monticulo
        sand = P(PrimitiveType.Cylinder, spin, new Vector3(0f, -0.1f, 0f), new Vector3(0.025f, 0.2f, 0.025f), star).transform;
        P(PrimitiveType.Sphere, spin, new Vector3(0f, -0.52f, 0f), new Vector3(0.34f, 0.16f, 0.34f), star);

        // halo cosmico
        var tilt = new GameObject("HaloInclinado").transform;
        tilt.SetParent(root, false);
        tilt.localRotation = Quaternion.Euler(70f, 0f, 20f);
        var halo = new GameObject("Halo").transform;
        halo.SetParent(tilt, false);
        var hs = halo.gameObject.AddComponent<TimeSpin>(); hs.Setup(TimeSpin.Mode.Spin, Vector3.up, 60f); hs.bossSync = false;
        for (int i = 0; i < 18; i++)
        {
            float a = i * Mathf.PI * 2f / 18f;
            P(PrimitiveType.Sphere, halo, new Vector3(Mathf.Sin(a), 0f, Mathf.Cos(a)) * 0.78f, Vector3.one * (i % 3 == 0 ? 0.07f : 0.04f), i % 2 == 0 ? neb1 : neb2);
        }

        // chispas que orbitan
        for (int i = 0; i < 8; i++)
        {
            var sp = P(PrimitiveType.Sphere, root, Vector3.zero, Vector3.one * 0.045f, i % 2 == 0 ? star : neb2);
            var o = sp.AddComponent<OrbitSpark>();
            o.radius = 0.55f + 0.12f * (i % 3); o.speed = 70f + 25f * i; o.height = 0.5f * ((i % 4) - 1.5f) * 0.6f; o.phase = i * 45f;
        }

        // luz violeta
        var lg = new GameObject("LuzAscua");
        lg.transform.SetParent(root, false);
        var l = lg.AddComponent<Light>();
        l.type = LightType.Point; l.range = 7f; l.intensity = 2.2f; l.color = new Color(0.7f, 0.5f, 1f);
        l.shadows = LightShadows.None;
    }

    void Update()
    {
        t += CosmosTime.Dt;
        if (sand != null)
        {
            float k = 0.75f + 0.25f * Mathf.Sin(t * 14f);
            sand.localScale = new Vector3(0.025f * k, 0.2f, 0.025f * k);
        }
    }
}

public class OrbitSpark : MonoBehaviour
{
    public float radius = 0.6f, speed = 90f, height = 0f, phase = 0f;
    float a;

    void Update()
    {
        a += speed * CosmosTime.Dt;
        float r = (a + phase) * Mathf.Deg2Rad;
        transform.localPosition = new Vector3(Mathf.Sin(r) * radius, height + Mathf.Sin(r * 2f) * 0.08f, Mathf.Cos(r) * radius);
    }
}
