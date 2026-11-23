using UnityEngine;

// Lluvia de bolas de fuego desde lo alto. Explotan al chocar (area chica y golpe suave). Caen rapido (no se ven venir hasta tarde), apuntadas
// un poco por delante del jugador, y con altura aumentan la frecuencia. Se rompen con la magia del mago.
public class FallingRockSpawner : MonoBehaviour
{
    public PartyManager party;
    public float baseInterval = 3.6f;    // segundos entre rocas en la zona mas baja
    public float minInterval = 2.3f;     // segundos entre rocas en la cima
    public float minAltitude = 4f;       // no caen rocas en el suelo de la base
    public float maxAltitude = 24f;
    public float height = 30f;           // altura de aparicion sobre el jugador
    public float maxRadius = 9f;         // dispersion horizontal alrededor del objetivo
    public float fallSpeed = 12f;        // velocidad inicial hacia abajo
    public Vector2 sizeRange = new Vector2(0.55f, 1.3f);
    public float leadTime = 0f;      // 0 = no apunta a donde vas        // apunta a donde estara el jugador

    public float minX = -1e9f;           // solo caen bolas pasada esta X (la aldea es segura)

    public float intensity = 1f;         // lo controla AvalancheDirector (mas alto = mas rocas)

    float timer;

    void Update()
    {
        if (party == null) return;
        var p = party.Current;
        Vector3 pos = p.transform.position;
        if (pos.y < minAltitude || pos.x < minX) return;

        float k = Mathf.InverseLerp(minAltitude, maxAltitude, pos.y);
        float interval = Mathf.Lerp(baseInterval, minInterval, k) / Mathf.Max(0.1f, intensity);

        timer += Time.deltaTime;
        if (timer < interval) return;
        timer = 0f;

        Vector3 vel = p.Controller != null ? p.Controller.velocity : Vector3.zero;
        vel.y = 0f;
        Vector3 aim = pos + vel * leadTime;
        // caen ALREDEDOR del jugador (anillo), y a veces casi encima: nunca exactas
        Vector2 o;
        if (Random.value < 0.22f) o = Random.insideUnitCircle * 1.8f;
        else
        {
            float ang = Random.value * Mathf.PI * 2f, rad = Random.Range(3.5f, maxRadius + 3f);
            o = new Vector2(Mathf.Cos(ang), Mathf.Sin(ang)) * rad;
        }
        float size = Random.Range(sizeRange.x, sizeRange.y);

        var go = GameObject.CreatePrimitive(PrimitiveType.Sphere);
        go.name = "BolaDeFuego";
        go.transform.position = new Vector3(aim.x + o.x, pos.y + height, aim.z + o.y);
        go.transform.localScale = Vector3.one * size;
        go.GetComponent<Renderer>().material.color = new Color(1f, 0.42f, 0.05f);

        go.AddComponent<ModelSlot>().slotName = "BolaFuego";
        var rb = go.AddComponent<Rigidbody>();
        rb.mass = Mathf.Max(2f, size * size * size * 80f);
        rb.collisionDetectionMode = CollisionDetectionMode.ContinuousDynamic;
        rb.linearVelocity = new Vector3(Random.Range(-2f, 2f), -fallSpeed * Random.Range(0.85f, 1.2f), Random.Range(-2f, 2f));

        var rock = go.AddComponent<FallingRock>();
        rock.party = party;
        rock.hp = size > 1.05f ? 2 : 1;
        rock.marker = MakeMarker(go.transform.position, size);
    }

    // Sombra roja en el suelo: te deja ver donde va a caer la roca para esquivarla
    GameObject MakeMarker(Vector3 from, float size)
    {
        var hits = Physics.RaycastAll(from, Vector3.down, 120f, ~0, QueryTriggerInteraction.Ignore);
        float best = float.MaxValue;
        Vector3 point = Vector3.zero;
        bool found = false;
        foreach (var h in hits)
        {
            if (h.collider.GetComponentInParent<FallingRock>() != null) continue;
            if (h.collider.GetComponentInParent<PlayerMovement>() != null) continue;
            if (h.collider.name == "Trozo") continue;
            if (h.distance < best) { best = h.distance; point = h.point; found = true; }
        }
        if (!found) return null;

        var d = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
        d.name = "SombraRoca";
        Destroy(d.GetComponent<Collider>());
        d.transform.position = point + Vector3.up * 0.04f;
        d.transform.localScale = new Vector3(size * 1.8f, 0.01f, size * 1.8f);
        d.GetComponent<Renderer>().material.color = new Color(0.9f, 0.15f, 0.1f);
        return d;
    }
}

public class FallingRock : MonoBehaviour
{
    public PartyManager party;
    public int hp = 1;
    public float life = 16f;
    public float knockback = 4f;
    public float explosionRadius = 3f;
    public GameObject marker;          // sombra en el suelo que avisa donde cae

    Rigidbody rb;
    float radius;
    bool done;

    void Start()
    {
        rb = GetComponent<Rigidbody>();
        radius = transform.localScale.x * 0.5f;
        Destroy(gameObject, life);
    }

    void OnDestroy()
    {
        if (marker != null) Destroy(marker);
    }

    void Update()
    {
        if (done || party == null || rb == null) return;
        if (rb.linearVelocity.magnitude < 4f) return;

        var p = party.Current;
        if (Vector3.Distance(transform.position, p.transform.position) < radius + 0.6f) Explode();
    }

    void OnCollisionEnter(Collision c)
    {
        if (done) return;
        if (c.collider.GetComponentInParent<FallingRock>() != null) return;
        if (c.collider.name == "Trozo") return;
        Explode();
    }

    // La magia (o un cubo) la destruye antes de que explote: sin dano, a veces suelta una moneda
    public void Hit(int damage = 1)
    {
        if (done) return;
        hp -= damage;
        FireSfx.Play(transform.position, 0.5f);
        if (hp > 0) return;

        done = true;
        if (Random.value < 0.5f) Coin.Spawn(transform.position + Vector3.up * 0.3f, 1);
        ExplosionFx.Spawn(transform.position, explosionRadius * 0.5f);
        Destroy(gameObject);
    }

    // Explota: dano solo si estas dentro del radio. Empuje suave.
    void Explode()
    {
        done = true;
        Vector3 c = transform.position;
        ExplosionFx.Spawn(c, explosionRadius);
        FireSfx.Play(c, 0.9f);

        if (party != null)
        {
            var p = party.Current;
            Vector3 to = p.transform.position - c;
            if (to.magnitude < explosionRadius)
            {
                to.y = 0f;
                if (to.sqrMagnitude < 0.01f) to = Random.insideUnitSphere;
                to.y = 0f;
                PlayerHealth.Hit(p, to.normalized * knockback);
            }
        }
        Destroy(gameObject);
    }
}

// Destello de explosion: una esfera naranja que crece y desaparece
public class ExplosionFx : MonoBehaviour
{
    float t, radius;

    public static void Spawn(Vector3 pos, float radius)
    {
        var go = GameObject.CreatePrimitive(PrimitiveType.Sphere);
        go.name = "Explosion";
        Destroy(go.GetComponent<Collider>());
        go.transform.position = pos;
        go.transform.localScale = Vector3.one * 0.3f;
        go.GetComponent<Renderer>().material.color = new Color(1f, 0.55f, 0.1f);
        go.AddComponent<ExplosionFx>().radius = radius;
    }

    void Update()
    {
        t += Time.deltaTime / 0.3f;
        transform.localScale = Vector3.one * Mathf.Lerp(0.3f, radius * 2f, Mathf.Sqrt(Mathf.Clamp01(t)));
        if (t >= 1f) Destroy(gameObject);
    }
}
