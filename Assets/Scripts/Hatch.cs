using System.Collections;
using TMPro;
using UnityEngine;

// Escotilla de acero con marco de franjas amarillas/negras, remaches, rueda central y luz roja por las rendijas.
// Reemplaza el aspecto del "crater" para que se entienda que hay que tocar el suelo. Se abre al arrojar el Ascua.
public class Hatch : MonoBehaviour
{
    Transform lidPivot;
    Material seam;
    Light glow;
    TMP_Text label;
    bool opened;

    public static Hatch Build(Transform crater)
    {
        float R = Mathf.Max(1.5f, crater.lossyScale.x * 0.5f);
        var go = new GameObject("Escotilla");
        go.transform.position = crater.position;
        var h = go.AddComponent<Hatch>();
        h.Make(R);
        return h;
    }

    static Material M(Color c, float metal, float smooth, float emis)
    {
        var t = GameObject.CreatePrimitive(PrimitiveType.Cube);
        var m = new Material(t.GetComponent<Renderer>().sharedMaterial) { color = c };
        Destroy(t);
        if (m.HasProperty("_Metallic")) m.SetFloat("_Metallic", metal);
        if (m.HasProperty("_Smoothness")) m.SetFloat("_Smoothness", smooth);
        if (emis > 0f) { m.EnableKeyword("_EMISSION"); if (m.HasProperty("_EmissionColor")) m.SetColor("_EmissionColor", c * emis); }
        return m;
    }

    GameObject P(PrimitiveType t, Transform parent, Vector3 pos, Vector3 scl, Material m, Vector3? euler = null)
    {
        var g = GameObject.CreatePrimitive(t);
        var col = g.GetComponent<Collider>(); if (col != null) Destroy(col);
        g.transform.SetParent(parent, false);
        g.transform.localPosition = pos;
        g.transform.localScale = scl;
        if (euler.HasValue) g.transform.localRotation = Quaternion.Euler(euler.Value);
        var r = g.GetComponent<Renderer>();
        r.sharedMaterial = m;
        r.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
        return g;
    }

    void Make(float R)
    {
        var steel = M(new Color(0.34f, 0.36f, 0.4f), 0.85f, 0.55f, 0f);
        var dark = M(new Color(0.12f, 0.12f, 0.14f), 0.6f, 0.4f, 0f);
        var yellow = M(new Color(0.95f, 0.75f, 0.1f), 0.2f, 0.4f, 0f);
        var rivet = M(new Color(0.6f, 0.6f, 0.65f), 0.9f, 0.7f, 0f);
        seam = M(new Color(1f, 0.3f, 0.05f), 0f, 0.5f, 2.2f);

        // resplandor por la rendija (bajo el marco)
        P(PrimitiveType.Cylinder, transform, new Vector3(0f, 0.06f, 0f), new Vector3(R * 1.85f, 0.04f, R * 1.85f), seam);
        // base oscura
        P(PrimitiveType.Cylinder, transform, new Vector3(0f, 0.05f, 0f), new Vector3(R * 2.15f, 0.05f, R * 2.15f), dark);

        // marco con franjas amarillas y negras
        int n = 24;
        for (int i = 0; i < n; i++)
        {
            float a = i * 360f / n;
            Vector3 dir = Quaternion.Euler(0f, a, 0f) * Vector3.forward;
            var seg = P(PrimitiveType.Cube, transform, dir * R * 0.97f + Vector3.up * 0.14f,
                new Vector3(R * 0.27f, 0.22f, R * 0.2f), i % 2 == 0 ? yellow : dark, new Vector3(0f, a, 0f));
        }

        // tapa (con bisagra: pivote en el borde -X)
        lidPivot = new GameObject("TapaPivote").transform;
        lidPivot.SetParent(transform, false);
        lidPivot.localPosition = new Vector3(-R * 0.82f, 0.3f, 0f);
        Vector3 c = new Vector3(R * 0.82f, 0f, 0f);   // centro de la tapa respecto al pivote
        P(PrimitiveType.Cylinder, lidPivot, c, new Vector3(R * 1.64f, 0.1f, R * 1.64f), steel);
        P(PrimitiveType.Cube, lidPivot, c + new Vector3(0f, 0.12f, 0f), new Vector3(R * 1.5f, 0.07f, 0.25f), dark);
        P(PrimitiveType.Cube, lidPivot, c + new Vector3(0f, 0.12f, 0f), new Vector3(0.25f, 0.07f, R * 1.5f), dark);
        for (int i = 0; i < 12; i++)
        {
            float a = i * 30f * Mathf.Deg2Rad;
            P(PrimitiveType.Sphere, lidPivot, c + new Vector3(Mathf.Sin(a), 0.1f, Mathf.Cos(a)) * (R * 0.72f), Vector3.one * 0.2f, rivet);
        }
        // rueda central
        P(PrimitiveType.Cylinder, lidPivot, c + new Vector3(0f, 0.2f, 0f), new Vector3(0.45f, 0.1f, 0.45f), yellow);
        for (int i = 0; i < 12; i++)
        {
            float a = i * 30f;
            Vector3 d = Quaternion.Euler(0f, a, 0f) * Vector3.forward;
            P(PrimitiveType.Cube, lidPivot, c + d * 0.62f + new Vector3(0f, 0.3f, 0f), new Vector3(0.16f, 0.14f, 0.3f), dark, new Vector3(0f, a, 0f));
        }
        for (int i = 0; i < 4; i++)
            P(PrimitiveType.Cube, lidPivot, c + new Vector3(0f, 0.3f, 0f), new Vector3(1.3f, 0.1f, 0.12f), dark, new Vector3(0f, i * 45f, 0f));

        // bisagras
        for (int s = -1; s <= 1; s += 2)
            P(PrimitiveType.Cylinder, transform, new Vector3(-R * 0.9f, 0.3f, s * R * 0.35f), new Vector3(0.28f, 0.45f, 0.28f), dark, new Vector3(90f, 0f, 0f));

        // luz
        var lg = new GameObject("LuzEscotilla");
        lg.transform.SetParent(transform, false);
        lg.transform.localPosition = new Vector3(0f, 1.2f, 0f);
        glow = lg.AddComponent<Light>();
        glow.type = LightType.Point; glow.range = R * 4f; glow.intensity = 6f; glow.color = new Color(1f, 0.4f, 0.1f);

        // cartel que solo aparece cerca
        var tgo = new GameObject("CartelEscotilla");
        tgo.transform.SetParent(transform, false);
        tgo.transform.localPosition = new Vector3(0f, 3.2f, 0f);
        var t = tgo.AddComponent<TextMeshPro>();
        t.text = "ESCOTILLA DEL CRATER\nTocala llevando el Ascua";
        t.fontSize = 4.5f;
        t.alignment = TextAlignmentOptions.Center;
        t.color = new Color(1f, 0.85f, 0.3f);
        t.rectTransform.sizeDelta = new Vector2(10f, 2f);
        tgo.AddComponent<ProximityLabel>().radius = 28f;
        label = t;
    }

    void Update()
    {
        if (opened || seam == null) return;
        float k = 0.6f + 0.4f * Mathf.Sin(Time.time * 3f);
        var c = new Color(1f, 0.3f, 0.05f);
        if (seam.HasProperty("_EmissionColor")) seam.SetColor("_EmissionColor", c * (1f + 2f * k));
        if (glow != null) glow.intensity = 4f + 4f * k;
    }

    public void Open()
    {
        if (opened) return;
        opened = true;
        if (label != null) label.gameObject.SetActive(false);
        StartCoroutine(Lift());
    }

    IEnumerator Lift()
    {
        float t = 0f;
        while (t < 1f)
        {
            t += Time.deltaTime / 1.4f;
            if (lidPivot != null) lidPivot.localRotation = Quaternion.Euler(0f, 0f, 110f * Mathf.SmoothStep(0f, 1f, t));
            yield return null;
        }
        if (seam != null && seam.HasProperty("_EmissionColor")) seam.SetColor("_EmissionColor", new Color(1f, 0.3f, 0.05f) * 4f);
        if (glow != null) glow.intensity = 14f;
    }
}
