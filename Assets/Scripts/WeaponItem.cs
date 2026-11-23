using UnityEngine;

// Arma del Peleador (VR): se agarra con el grip, hiere por velocidad de golpe, se puede lanzar.
// El eje +Z local es la hoja; el agarre esta en el origen. Se construye con primitivas (sin modelos).
[RequireComponent(typeof(Rigidbody))]
public class WeaponItem : MonoBehaviour
{
    public enum Kind { Sword, Axe, Mace, Shield }
    public Kind kind;
    public int damage = 1;
    public float length = 1f;

    public bool Held { get; private set; }
    Rigidbody rb;
    Collider[] cols;
    Vector3 prevTip, tipVel, spawn;
    Transform hand;
    static readonly System.Collections.Generic.List<WeaponItem> held = new System.Collections.Generic.List<WeaponItem>();

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    static void Reset() { held.Clear(); }

    // true si un escudo sostenido esta de frente al atacante
    public static bool Blocks(Vector3 attackerPos)
    {
        foreach (var w in held)
        {
            if (w == null || w.kind != Kind.Shield) continue;
            Vector3 d = attackerPos - w.transform.position;
            d.y = 0f;
            Vector3 f = w.transform.forward; f.y = 0f;
            if (d.sqrMagnitude > 0.01f && f.sqrMagnitude > 0.01f && Vector3.Dot(d.normalized, f.normalized) > 0.35f) return true;
        }
        return false;
    }

    public static WeaponItem Make(Kind k, Vector3 pos)
    {
        var root = new GameObject("Arma_" + k);
        root.transform.position = pos;
        root.transform.rotation = Quaternion.Euler(0f, Random.Range(0f, 360f), 0f);
        var wood = new Color(0.35f, 0.2f, 0.1f);
        var steel = new Color(0.78f, 0.8f, 0.85f);
        var dark = new Color(0.25f, 0.26f, 0.3f);
        var w = root.AddComponent<WeaponItem>();
        w.kind = k;
        var box = root.AddComponent<BoxCollider>();

        switch (k)
        {
            case Kind.Sword:
                Part(root, PrimitiveType.Cylinder, new Vector3(0f, 0f, 0f), new Vector3(0.05f, 0.11f, 0.05f), new Vector3(90f, 0f, 0f), wood);
                Part(root, PrimitiveType.Cube, new Vector3(0f, 0f, 0.12f), new Vector3(0.28f, 0.04f, 0.05f), Vector3.zero, new Color(0.8f, 0.65f, 0.2f));
                Part(root, PrimitiveType.Cube, new Vector3(0f, 0f, 0.62f), new Vector3(0.07f, 0.015f, 1f), Vector3.zero, steel);
                w.damage = 1; w.length = 1.1f;
                box.center = new Vector3(0f, 0f, 0.5f); box.size = new Vector3(0.28f, 0.06f, 1.2f);
                break;
            case Kind.Axe:
                Part(root, PrimitiveType.Cylinder, new Vector3(0f, 0f, 0.4f), new Vector3(0.05f, 0.5f, 0.05f), new Vector3(90f, 0f, 0f), wood);
                Part(root, PrimitiveType.Cube, new Vector3(0f, 0.12f, 0.78f), new Vector3(0.04f, 0.36f, 0.3f), Vector3.zero, steel);
                w.damage = 2; w.length = 1f;
                box.center = new Vector3(0f, 0.05f, 0.45f); box.size = new Vector3(0.1f, 0.4f, 1.0f);
                break;
            case Kind.Mace:
                Part(root, PrimitiveType.Cylinder, new Vector3(0f, 0f, 0.35f), new Vector3(0.06f, 0.4f, 0.06f), new Vector3(90f, 0f, 0f), wood);
                Part(root, PrimitiveType.Sphere, new Vector3(0f, 0f, 0.82f), Vector3.one * 0.3f, Vector3.zero, dark);
                w.damage = 3; w.length = 1f;
                box.center = new Vector3(0f, 0f, 0.5f); box.size = new Vector3(0.3f, 0.3f, 1.0f);
                break;
            default:
                Part(root, PrimitiveType.Cylinder, new Vector3(0f, 0f, 0.12f), new Vector3(0.75f, 0.03f, 0.75f), new Vector3(90f, 0f, 0f), new Color(0.55f, 0.15f, 0.12f));
                Part(root, PrimitiveType.Sphere, new Vector3(0f, 0f, 0.17f), Vector3.one * 0.18f, Vector3.zero, steel);
                Part(root, PrimitiveType.Cube, new Vector3(0f, 0f, 0f), new Vector3(0.05f, 0.2f, 0.05f), Vector3.zero, wood);
                w.damage = 1; w.length = 0.4f;
                box.center = new Vector3(0f, 0f, 0.1f); box.size = new Vector3(0.75f, 0.75f, 0.1f);
                break;
        }
        var rb = root.GetComponent<Rigidbody>();
        rb.mass = 2f;
        rb.interpolation = RigidbodyInterpolation.Interpolate;
        rb.collisionDetectionMode = CollisionDetectionMode.ContinuousDynamic;
        return w;
    }

    static void Part(GameObject root, PrimitiveType t, Vector3 pos, Vector3 scale, Vector3 euler, Color c)
    {
        var g = GameObject.CreatePrimitive(t);
        Destroy(g.GetComponent<Collider>());
        g.transform.SetParent(root.transform, false);
        g.transform.localPosition = pos;
        g.transform.localEulerAngles = euler;
        g.transform.localScale = scale;
        var r = g.GetComponent<Renderer>();
        r.material.color = c;
    }

    void Awake()
    {
        rb = GetComponent<Rigidbody>();
        cols = GetComponents<Collider>();
        spawn = transform.position;
    }

    public void Hold(Transform h)
    {
        Held = true;
        hand = h;
        rb.isKinematic = true;
        foreach (var c in cols) c.isTrigger = true;
        transform.SetParent(h, false);
        transform.localPosition = kind == Kind.Shield ? new Vector3(0f, 0f, 0.12f) : Vector3.zero;
        transform.localRotation = kind == Kind.Shield ? Quaternion.identity : Quaternion.Euler(-40f, 0f, 0f);
        prevTip = Tip();
        held.Add(this);
    }

    public void Release(Vector3 vel)
    {
        Held = false;
        hand = null;
        held.Remove(this);
        transform.SetParent(null, true);
        rb.isKinematic = false;
        foreach (var c in cols) c.isTrigger = false;
        rb.linearVelocity = vel;
        rb.angularVelocity = Random.insideUnitSphere * 3f;
    }

    Vector3 Tip() => transform.TransformPoint(0f, 0f, length);

    void Update()
    {
        if (!Held)
        {
            if (transform.position.y < spawn.y - 40f) { rb.linearVelocity = Vector3.zero; transform.position = spawn; }
            return;
        }
        float dt = Mathf.Max(Time.deltaTime, 0.0001f);
        Vector3 tip = Tip();
        tipVel = Vector3.Lerp(tipVel, (tip - prevTip) / dt, 0.5f);
        prevTip = tip;
        float speed = tipVel.magnitude;
        if (speed < 2.2f) return;

        var hits = Physics.OverlapCapsule(transform.position, tip, kind == Kind.Shield ? 0.4f : 0.15f, ~0, QueryTriggerInteraction.Ignore);
        foreach (var c in hits)
        {
            var e = c.GetComponentInParent<Enemy>();
            if (e == null) continue;
            int d = damage + (speed > 5f ? 1 : 0);
            e.Hurt(kind == Kind.Shield ? 1 : d, tipVel);
        }
    }

    void OnCollisionEnter(Collision c)
    {
        if (Held || c.relativeVelocity.magnitude < 4f) return;
        var e = c.collider.GetComponentInParent<Enemy>();
        if (e != null) e.Hurt(damage + 1, c.relativeVelocity);
    }
}
