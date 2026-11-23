using UnityEngine;

// Interaccion 4 (Mago): proyectil magico. Se crea por codigo, no necesita prefab.
public static class MagicShooter
{
    static AudioClip shotClip;

    public static void Fire(Vector3 origin, Vector3 direction, float speed = 32f, int power = 1)
    {
        int total = power + RunState.MagicBonus;
        var go = PropModels.BuildBolt(origin, direction, total);
        var mp = go.AddComponent<MagicProjectile>();
        mp.speed = speed;
        mp.power = total;

        // sonido del disparo (Resources/Audio/DisparoMago); si no esta, tono corto
        if (shotClip == null) shotClip = Resources.Load<AudioClip>("Audio/DisparoMago");
        if (shotClip != null) AudioSource.PlayClipAtPoint(shotClip, origin, 1f);
        else Sfx.Beep(900f, 0.12f);
    }
}

public class MagicProjectile : MonoBehaviour
{
    public float speed = 32f;
    public float life = 3f;
    public int power = 1;

    void Start()
    {
        var col = GetComponent<Collider>();
        if (col != null) col.isTrigger = true;

        var rb = GetComponent<Rigidbody>();
        if (rb == null) rb = gameObject.AddComponent<Rigidbody>();
        rb.isKinematic = true;
        rb.useGravity = false;

        Destroy(gameObject, life);
    }

    void Update()
    {
        transform.position += transform.forward * speed * Time.deltaTime;
    }

    void OnTriggerEnter(Collider other)
    {
        if (other.isTrigger) return;
        if (other.GetComponentInParent<PlayerMovement>() != null) return;

        if (other.name == "Trozo") return;   // los pedazos de roca rota no frenan la magia

        var crystal = other.GetComponentInParent<Crystal>();
        if (crystal != null) crystal.Hit();

        var boss = other.GetComponentInParent<BossController>();
        if (boss != null) boss.Hit(power);

        var enemy = other.GetComponentInParent<Enemy>();
        if (enemy != null) enemy.Hurt(power, transform.forward);

        var rock = other.GetComponentInParent<FallingRock>();
        if (rock != null) rock.Hit(power);

        PropModels.Burst(transform.position, power > 1 ? new Color(1f, 0.55f, 0.95f) : new Color(0.4f, 0.9f, 1f), 8);
        Destroy(gameObject);
    }
}
