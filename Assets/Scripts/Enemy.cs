using System.Collections.Generic;
using UnityEngine;

// Orco enemigo: persigue al personaje activo, ataca cuerpo a cuerpo y suelta monedas al morir.
// Solo aparece con el casco VR puesto (o con el modo desarrollador). Lo hieren armas, magia y cubos lanzados.
public class Enemy : MonoBehaviour
{
    public int hp = 3;
    public float speed = 2.2f;
    public float aggroRange = 15f;
    public float attackRange = 1.7f;
    public float attackCooldown = 1.6f;
    public int coins = 2;
    public string label = "Orco";

    int maxHp;
    float nextAttack, hurtUntil, invulUntil, strikeAt = -1f;
    bool dead;
    PartyManager party;
    ActorAnim anim;
    Transform bar, fill;
    Vector3 home;
    CharacterController cc;
    Transform vis;
    Vector3 visBase, wantMove, knock;
    float vy;
    bool moving;
    Transform sword; Quaternion swordBase;      // espada del zombi (pivote en la mano) para la animacion de ataque
    const float AtkDur = 0.75f;               // duracion del golpe sin esqueleto (alzar y descargar)
    float recoverUntil;

    public static readonly List<Enemy> All = new List<Enemy>();

    void OnEnable() => All.Add(this);
    void OnDisable() => All.Remove(this);

    void Start()
    {
        maxHp = hp;
        home = transform.position;
        party = FindFirstObjectByType<PartyManager>();
        anim = GetComponent<ActorAnim>();
        if (anim != null) { anim.manual = true; anim.Play("Sword_Idle", 0f); }
        vis = transform.Find("Modelo");
        if (vis != null)
        {
            visBase = vis.localPosition;
            foreach (var tr in vis.GetComponentsInChildren<Transform>(true))
                if (tr.name == "EspadaMano") { sword = tr; swordBase = tr.localRotation; break; }
        }

        // el cuerpo fisico es un CharacterController: choca con muros y suelo (no atraviesa nada)
        var cap = GetComponent<CapsuleCollider>();
        if (cap != null) Destroy(cap);
        cc = GetComponent<CharacterController>();
        if (cc == null)
        {
            cc = gameObject.AddComponent<CharacterController>();
            cc.height = 1.9f; cc.radius = 0.4f; cc.center = new Vector3(0f, 0.95f, 0f);
            cc.stepOffset = 0.3f; cc.skinWidth = 0.05f;
        }
        MakeBar();
    }

    void MakeBar()
    {
        var root = new GameObject("Vida").transform;
        root.SetParent(transform, false);
        root.localPosition = new Vector3(0f, 2.15f, 0f);
        bar = root;
        var bg = GameObject.CreatePrimitive(PrimitiveType.Cube);
        Destroy(bg.GetComponent<Collider>());
        bg.transform.SetParent(root, false);
        bg.transform.localScale = new Vector3(0.9f, 0.1f, 0.02f);
        bg.GetComponent<Renderer>().material.color = new Color(0.1f, 0.1f, 0.1f);
        var f = GameObject.CreatePrimitive(PrimitiveType.Cube);
        Destroy(f.GetComponent<Collider>());
        f.transform.SetParent(root, false);
        f.transform.localPosition = new Vector3(0f, 0f, -0.012f);
        f.GetComponent<Renderer>().material.color = new Color(0.85f, 0.15f, 0.1f);
        fill = f.transform;
        SetBar();
    }

    void SetBar()
    {
        if (fill == null) return;
        float t = Mathf.Clamp01((float)hp / Mathf.Max(1, maxHp));
        fill.localScale = new Vector3(0.86f * t, 0.07f, 0.02f);
        fill.localPosition = new Vector3(-0.43f * (1f - t), 0f, -0.012f);
    }

    void Update()
    {
        if (dead) return;
        if (bar != null && Camera.main != null)
        {
            Vector3 d = bar.position - Camera.main.transform.position;
            d.y = 0f;
            if (d.sqrMagnitude > 0.01f) bar.rotation = Quaternion.LookRotation(d);
        }
        if (party == null) { party = FindFirstObjectByType<PartyManager>(); return; }
        if (Time.time < hurtUntil) return;

        var p = party.Current;
        Vector3 to = p.transform.position - transform.position;
        float dy = Mathf.Abs(to.y);
        to.y = 0f;
        float dist = to.magnitude;

        // golpe pendiente (el impacto ocurre a mitad de la animacion)
        if (strikeAt > 0f && Time.time >= strikeAt)
        {
            strikeAt = -1f;
            recoverUntil = Time.time + 0.4f;
            if (dist < attackRange + 0.6f && dy < 2.5f && !WeaponItem.Blocks(transform.position))
                PlayerHealth.Hit(p, to.normalized * 5f, label + "!");
            else if (dist < attackRange + 0.6f) { HUD.Say("¡Bloqueado!"); Sfx.Beep(300f, 0.08f); }
        }
        if (strikeAt > 0f) return;

        if (dist > aggroRange || dy > 6f)
        {
            // vuelve a su puesto
            Vector3 back = home - transform.position; back.y = 0f;
            if (back.magnitude > 1f) Walk(back.normalized, 0.6f); else Idle();
            return;
        }
        if (dist > attackRange) { Walk(to.normalized, 1f); return; }

        Face(to);
        if (Time.time >= nextAttack)
        {
            nextAttack = Time.time + attackCooldown;
            strikeAt = Time.time + (anim != null ? 0.45f : AtkDur);
            if (anim != null) anim.Force("Sword_Attack", 0.08f);
        }
        else Idle();
    }

    void Walk(Vector3 dir, float mul)
    {
        Face(dir);
        wantMove = dir * speed * mul;
        moving = true;
        if (anim != null) anim.Play("Walk_Loop", 0.15f);
    }

    // movimiento con colision + animacion procedural (si el modelo no tiene esqueleto)
    void LateUpdate()
    {
        if (dead) return;
        if (cc != null && cc.enabled)
        {
            vy = cc.isGrounded ? -1f : vy - 20f * Time.deltaTime;
            cc.Move((wantMove + knock + Vector3.up * vy) * Time.deltaTime);
        }
        wantMove = Vector3.zero;
        knock = Vector3.Lerp(knock, Vector3.zero, 8f * Time.deltaTime);

        if (vis != null && anim == null)
        {
            float t = Time.time;
            float bob = moving ? Mathf.Abs(Mathf.Sin(t * 7f)) * 0.07f : 0f;
            float roll = moving ? Mathf.Sin(t * 3.5f) * 5f : Mathf.Sin(t * 1.2f) * 1.5f;
            float pitch = moving ? 8f : 0f;
            float swordAng = 0f, lunge = 0f;
            if (strikeAt > 0f)
            {
                // ataque: se echa atras y alza la espada (60 %), luego descarga hacia adelante
                float p = Mathf.Clamp01(1f - (strikeAt - t) / AtkDur);
                if (p < 0.6f) { float k = p / 0.6f; k = k * k * (3f - 2f * k); swordAng = -135f * k; pitch = -14f * k; }
                else { float k = (p - 0.6f) / 0.4f; k *= k; swordAng = Mathf.Lerp(-135f, 40f, k); pitch = Mathf.Lerp(-14f, 34f, k); lunge = 0.3f * k; }
            }
            else if (t < recoverUntil)
            {
                float k = 1f - (recoverUntil - t) / 0.4f;
                swordAng = Mathf.Lerp(40f, 0f, k); pitch = Mathf.Lerp(34f, 0f, k); lunge = Mathf.Lerp(0.3f, 0f, k);
            }
            if (t < hurtUntil) { pitch = -18f; swordAng = -20f; }
            if (sword != null) sword.localRotation = Quaternion.Euler(swordAng, 0f, 0f) * swordBase;
            vis.localPosition = visBase + Vector3.up * bob + Vector3.forward * lunge;
            vis.localRotation = Quaternion.Euler(pitch, 0f, roll);
        }
        moving = false;
    }

    void Idle() { if (anim != null) anim.Play("Sword_Idle", 0.2f); }

    void Face(Vector3 d)
    {
        d.y = 0f;
        if (d.sqrMagnitude < 0.0001f) return;
        transform.rotation = Quaternion.Slerp(transform.rotation, Quaternion.LookRotation(d), 8f * Time.deltaTime);
    }

    public void Hurt(int dmg, Vector3 dir)
    {
        if (dead || Time.time < invulUntil) return;
        invulUntil = Time.time + 0.35f;
        hp -= Mathf.Max(1, dmg);
        SetBar();
        Sfx.Beep(160f, 0.1f);
        dir.y = 0f;
        if (dir.sqrMagnitude > 0.001f) knock = dir.normalized * 6f;
        if (hp <= 0) { StartCoroutine(Die()); return; }
        hurtUntil = Time.time + 0.4f;
        strikeAt = -1f;
        if (anim != null) anim.Force("Hit_Chest", 0.05f);
    }

    System.Collections.IEnumerator Die()
    {
        dead = true;
        foreach (var c in GetComponentsInChildren<Collider>()) c.enabled = false;
        if (bar != null) bar.gameObject.SetActive(false);
        if (anim != null) anim.Force("Death01", 0.1f);
        else if (vis != null)
        {
            float rt = 0f;
            Quaternion from = vis.localRotation, to = Quaternion.Euler(-88f, 0f, 0f);
            while (rt < 1f) { rt += Time.deltaTime / 0.5f; vis.localRotation = Quaternion.Slerp(from, to, rt); yield return null; }
        }
        HUD.Say(label + " derrotado");
        Sfx.Beep(120f, 0.25f);
        for (int i = 0; i < coins; i++)
            Coin.Spawn(transform.position + new Vector3(Random.Range(-0.6f, 0.6f), 0.9f, Random.Range(-0.6f, 0.6f)), 1);
        yield return new WaitForSeconds(2.5f);
        float t = 0f;
        while (t < 1f) { t += Time.deltaTime; transform.position += Vector3.down * Time.deltaTime * 0.8f; yield return null; }
        Destroy(gameObject);
    }
}

// Coloca enemigos y armas al entrar a un piso (solo VR / modo desarrollador).
public static class EnemySpawner
{
    static readonly HashSet<int> done = new HashSet<int>();

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    static void Reset() { done.Clear(); }

    public static void Populate(MountainZone z)
    {
        if (z == null || z.content == null || done.Contains(z.index)) return;
        if (!VRRig.Active && !DevMode.Enabled) return;
        done.Add(z.index);
        int f = z.index;
        var rnd = new System.Random(f * 97 + 3);
        var orco = Resources.Load<GameObject>("Enemigos/Zombi");
        var bruto = Resources.Load<GameObject>("Enemigos/ZombiBruto");
        if (orco == null) orco = Resources.Load<GameObject>("Enemigos/Orco");
        if (bruto == null) bruto = Resources.Load<GameObject>("Enemigos/OrcoBruto");

        int n = 2 + f;
        for (int i = 0; i < n; i++)
        {
            bool big = f >= 3 && i < f - 2;
            var prefab = big && bruto != null ? bruto : orco;
            if (prefab == null) break;
            float x = i % 2 == 0 ? 11f + (float)rnd.NextDouble() * 16f : -(14f + (float)rnd.NextDouble() * 13f);
            float zz = ((float)rnd.NextDouble() * 2f - 1f) * 12f;
            var go = Object.Instantiate(prefab, z.content);
            go.transform.localPosition = new Vector3(x, 0.05f, zz);
            go.transform.localRotation = Quaternion.Euler(0f, rnd.Next(360), 0f);
            go.name = big ? "EnemigoBruto" : "Enemigo";
        }

        // armero cerca de la entrada del piso
        WeaponItem.Kind[] kinds = { WeaponItem.Kind.Sword, WeaponItem.Kind.Axe, WeaponItem.Kind.Mace, WeaponItem.Kind.Shield };
        for (int i = 0; i < 4; i++)
        {
            float x = -27f + i * 2.2f;
            float zz = (i % 2 == 0 ? 1f : -1f) * 12.5f;
            if (f > 1 && i == 0) continue;   // pisos altos: menos armas, hay que conservarlas
            var w = WeaponItem.Make(kinds[i], z.content.TransformPoint(new Vector3(x, 0.6f, zz)));
            w.transform.SetParent(z.content, true);
        }
        if (f == 1) HUD.Say("Peleador (solo VR): agarra armas con el grip y golpea orcos");
    }
}
