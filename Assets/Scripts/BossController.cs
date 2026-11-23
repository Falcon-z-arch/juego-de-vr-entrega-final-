using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;

// El Cronarca, guardian del tiempo. Flota sobre el crater.
//  - Manecillas: barras que giran a ras de suelo (salta para pasarlas). Se suman mas con cada fase.
//  - Bombas de tiempo: marcas rojas que explotan a los 2 s.
//  - Rebobinar: te devuelve a donde estabas hace 4 s.
// Se hiere con magia (1 por disparo, mas con poder) o lanzandole un cubo (4 de dano).
[RequireComponent(typeof(Rigidbody))]
public class BossController : MonoBehaviour
{
    public int hpMax = 24;
    public float floorY = 0f;           // altura del suelo de la arena (la fija el builder)
    public float handLength = 21f;

    int hp;
    PartyManager party;
    Renderer rend;
    Vector3 basePos;
    int phase = 0;
    readonly List<ClockHand> hands = new List<ClockHand>();
    readonly Queue<Vector3> history = new Queue<Vector3>();
    float histTimer, hitCooldown;
    TMP_Text bar;
    bool dead;
    Color baseColor;

    void Awake()
    {
        var rb = GetComponent<Rigidbody>();
        rb.isKinematic = true; rb.useGravity = false;
        rend = CronarcaLook.Build(transform);
        if (rend != null) baseColor = rend.material.color;
        basePos = transform.position;
    }

    void OnEnable()
    {
        if (hp > 0) return;
        hp = hpMax;
        party = FindFirstObjectByType<PartyManager>();
        MakeBar();
        SetPhase(1);
        StartCoroutine(Attacks());
        StartCoroutine(Rewinds());
    }

    void Update()
    {
        if (dead) return;
        transform.position = basePos + Vector3.up * Mathf.Sin(Time.time * 1.5f) * 0.4f;
        transform.Rotate(0f, 20f * Time.deltaTime, 0f);
        hitCooldown -= Time.deltaTime;

        // historial de posiciones para rebobinar (4 s)
        histTimer += Time.deltaTime;
        if (histTimer >= 0.25f && party != null)
        {
            histTimer = 0f;
            history.Enqueue(party.Current.transform.position);
            if (history.Count > 16) history.Dequeue();
        }
    }

    // ---------- dano ----------
    public void Hit(int damage)
    {
        if (dead || hitCooldown > 0f) return;
        hitCooldown = 0.15f;
        hp -= damage;
        Sfx.Beep(500f, 0.08f);
        StartCoroutine(Flash());
        UpdateBar();

        if (hp <= 0) { StartCoroutine(Die()); return; }
        int np = hp > hpMax * 2 / 3 ? 1 : hp > hpMax / 3 ? 2 : 3;
        if (np != phase) SetPhase(np);
    }

    void OnCollisionEnter(Collision c)
    {
        var cube = c.collider.GetComponentInParent<PuzzleCube>();
        if (cube != null && c.relativeVelocity.magnitude > 4f)
        {
            Hit(4);
            HUD.Say("El cubo golpeo al Cronarca!");
        }
    }

    IEnumerator Flash()
    {
        if (rend != null) rend.material.color = Color.white;
        yield return new WaitForSeconds(0.08f);
        if (rend != null && !dead) rend.material.color = baseColor;
    }

    // ---------- fases y ataques ----------
    void SetPhase(int p)
    {
        phase = p;
        CronarcaLook.Speed = 1f + 0.7f * (p - 1);
        foreach (var h in hands) if (h != null) Destroy(h.gameObject);
        hands.Clear();

        float speed = 20f + 8f * p;
        for (int i = 0; i < p; i++)
        {
            float angle = i * 360f / p;
            float dir = (i % 2 == 0) ? 1f : -1f;
            hands.Add(ClockHand.Create(new Vector3(basePos.x, floorY, basePos.z), handLength, angle, speed * dir, party));
        }
        if (p > 1) HUD.Say("El Cronarca acelera el tiempo! (fase " + p + ")");
    }

    IEnumerator Attacks()
    {
        yield return new WaitForSeconds(3f);
        while (!dead)
        {
            int n = 2 + phase;
            Vector3 pp = party.Current.transform.position;
            for (int i = 0; i < n; i++)
            {
                Vector2 o = i == 0 ? Vector2.zero : Random.insideUnitCircle * 5f;
                Vector3 pos = new Vector3(pp.x + o.x, floorY, pp.z + o.y);
                if ((new Vector2(pos.x - basePos.x, pos.z - basePos.z)).magnitude > handLength) continue;
                TimeBomb.Spawn(pos, 2f, 3f, party);
            }
            Sfx.Beep(300f, 0.15f);
            yield return new WaitForSeconds(Mathf.Max(2.5f, 7f - phase));
        }
    }

    IEnumerator Rewinds()
    {
        yield return new WaitForSeconds(12f);
        while (!dead)
        {
            HUD.Say("El Cronarca rebobina el tiempo...");
            Sfx.Beep(180f, 0.8f);
            yield return new WaitForSeconds(1.5f);
            if (dead) yield break;
            if (history.Count > 0)
            {
                Vector3 back = history.Peek();
                party.Current.Teleport(back);
                HUD.Say("El tiempo retrocede!");
            }
            yield return new WaitForSeconds(Mathf.Max(10f, 20f - 3f * phase));
        }
    }

    // ---------- muerte ----------
    IEnumerator Die()
    {
        dead = true;
        foreach (var h in hands) if (h != null) Destroy(h.gameObject);
        hands.Clear();
        foreach (var b in FindObjectsByType<TimeBomb>(FindObjectsSortMode.None)) Destroy(b.gameObject);

        if (bar != null) bar.text = "";
        HUD.Say("El Cronarca se hace pedazos");
        for (int i = 0; i < 6; i++)
        {
            ExplosionFx.Spawn(transform.position + Random.insideUnitSphere * 2f, 2.5f);
            Sfx.Beep(200f + i * 60f, 0.15f);
            yield return new WaitForSeconds(0.25f);
        }
        for (int i = 0; i < 14; i++) Coin.Spawn(transform.position + Random.insideUnitSphere * 2f, 3);

        var spawner = FindFirstObjectByType<FallingRockSpawner>();
        if (spawner != null) spawner.enabled = false;
        var dir = FindFirstObjectByType<AvalancheDirector>();
        if (dir != null) dir.enabled = false;
        foreach (var r in FindObjectsByType<FallingRock>(FindObjectsSortMode.None)) Destroy(r.gameObject);

        foreach (var e in Enemy.All.ToArray()) if (e != null) Destroy(e.gameObject);
        WindAmbience.Peace();   // queda solo el viento
        if (RunManager.I != null) RunManager.I.BossDefeated();
        gameObject.SetActive(false);
    }

    // ---------- barra de vida ----------
    void MakeBar()
    {
        var canvas = UiKit.MainCanvas();
        if (canvas == null) return;
        var go = new GameObject("BarraJefe", typeof(RectTransform));
        go.transform.SetParent(canvas.transform, false);
        var rt = (RectTransform)go.transform;
        rt.anchorMin = rt.anchorMax = new Vector2(0.5f, 1f);
        rt.pivot = new Vector2(0.5f, 1f);
        rt.anchoredPosition = new Vector2(0f, -130f);
        rt.sizeDelta = new Vector2(1300f, 80f);
        var t = go.AddComponent<TextMeshProUGUI>();
        t.fontSize = 40;
        t.alignment = TextAlignmentOptions.Center;
        t.color = new Color(1f, 0.5f, 0.3f);
        t.raycastTarget = false;
        bar = t;
        UpdateBar();
    }

    void UpdateBar()
    {
        if (bar == null) return;
        int n = Mathf.Clamp(Mathf.CeilToInt(hp * 20f / hpMax), 0, 20);
        bar.text = "EL CRONARCA  [" + new string('#', n) + new string('-', 20 - n) + "]";
    }

    void OnDestroy()
    {
        if (bar != null) Destroy(bar.gameObject);
    }
}

// Manecilla del reloj: barra a ras de suelo que gira. Saltala.
public class ClockHand : MonoBehaviour
{
    float length, speed, angle;
    Vector3 pivot;
    PartyManager party;

    public static ClockHand Create(Vector3 pivot, float length, float startAngle, float speed, PartyManager party)
    {
        var go = GameObject.CreatePrimitive(PrimitiveType.Cube);
        go.name = "Manecilla";
        Destroy(go.GetComponent<Collider>());
        go.GetComponent<Renderer>().material.color = new Color(0.95f, 0.8f, 0.3f);
        var h = go.AddComponent<ClockHand>();
        h.pivot = pivot; h.length = length; h.speed = speed; h.angle = startAngle; h.party = party;
        go.transform.localScale = new Vector3(0.6f, 0.5f, length);
        h.Place();
        return h;
    }

    void Place()
    {
        Quaternion rot = Quaternion.Euler(0f, angle, 0f);
        transform.rotation = rot;
        transform.position = pivot + rot * Vector3.forward * (length * 0.5f) + Vector3.up * 0.25f;
    }

    void Update()
    {
        angle += speed * Time.deltaTime;
        Place();
        if (party == null) return;

        var p = party.Current;
        var cc = p.Controller;
        if (cc == null) return;
        if (cc.bounds.min.y > pivot.y + 0.7f) return;      // vas en el aire: la pasas por arriba

        Vector3 fwd = transform.rotation * Vector3.forward;
        Vector3 rel = p.transform.position - pivot;
        float along = Vector3.Dot(rel, fwd);
        if (along < 0f || along > length) return;
        Vector3 side = rel - fwd * along;
        side.y = 0f;
        if (side.magnitude < 0.9f)
        {
            Vector3 tangent = Vector3.Cross(Vector3.up, fwd) * Mathf.Sign(speed);
            PlayerHealth.Hit(p, tangent.normalized * 4f);
        }
    }
}

// Bomba de tiempo: marca roja que parpadea y explota a los "delay" segundos.
public class TimeBomb : MonoBehaviour
{
    float delay, radius, t;
    PartyManager party;
    Renderer rend;

    public static void Spawn(Vector3 pos, float delay, float radius, PartyManager party)
    {
        var go = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
        go.name = "BombaTiempo";
        Destroy(go.GetComponent<Collider>());
        go.transform.position = pos + Vector3.up * 0.05f;
        go.transform.localScale = new Vector3(radius * 2f, 0.02f, radius * 2f);
        var b = go.AddComponent<TimeBomb>();
        b.delay = delay; b.radius = radius; b.party = party;
        b.rend = go.GetComponent<Renderer>();
    }

    void Update()
    {
        t += Time.deltaTime;
        float blink = Mathf.PingPong(t * (3f + t * 4f), 1f);
        rend.material.color = Color.Lerp(new Color(0.5f, 0.05f, 0.05f), new Color(1f, 0.3f, 0.1f), blink);

        if (t < delay) return;
        ExplosionFx.Spawn(transform.position + Vector3.up * 0.5f, radius);
        Sfx.Beep(240f, 0.12f);
        if (party != null)
        {
            var p = party.Current;
            Vector3 to = p.transform.position - transform.position;
            if (new Vector2(to.x, to.z).magnitude < radius && to.y < 3f)
            {
                to.y = 0f;
                if (to.sqrMagnitude < 0.01f) to = Vector3.forward;
                PlayerHealth.Hit(p, to.normalized * 4f);
            }
        }
        Destroy(gameObject);
    }
}
