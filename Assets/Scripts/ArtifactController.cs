using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;

// El artefacto: siempre lo llevas (se ve en un costado del personaje).
// Si lo sueltas y pasan mas de "loseTime" segundos (5 minutos) sin recogerlo, lo pierdes y se reinicia la escena.
// Cuando esta en el suelo se recoge tocandolo estando cerca.
public class ArtifactController : Interactable
{
    public PartyManager party;
    public float loseTime = 300f;                                   // 5 minutos
    public Vector3 carryOffset = new Vector3(0.65f, 0.1f, 0.1f);    // costado derecho del personaje
    public Vector3 firstPersonOffset = new Vector3(0.45f, -0.38f, 0.9f);   // esquina inferior derecha de la vista

    public bool IsCarried { get; private set; } = true;
    public bool IsGone { get; private set; }
    public float TimeLeft { get; private set; }

    Collider col;
    Vector3 baseScale;

    void Awake()
    {
        prompt = "Ascua";
        if (GetComponent<ArtifactVisual>() == null) gameObject.AddComponent<ArtifactVisual>();
        baseScale = transform.localScale;
        TimeLeft = loseTime;
        col = GetComponent<Collider>();
        if (col != null) col.enabled = false;   // llevado: no estorba ni se toca
    }

    public static string Format(float seconds)
    {
        int m = Mathf.Max(0, (int)(seconds / 60f));
        int s = Mathf.Max(0, (int)(seconds % 60f));
        return m + ":" + s.ToString("00");
    }

    public void AttachTo(Transform carrier)
    {
        if (IsGone) return;
        IsCarried = true;
        TimeLeft = loseTime;

        transform.SetParent(carrier, false);
        transform.localPosition = carrier.GetComponent<Camera>() != null ? firstPersonOffset : carryOffset;
        transform.localRotation = Quaternion.identity;
        Vector3 ls = carrier.lossyScale;
        transform.localScale = new Vector3(baseScale.x / ls.x, baseScale.y / ls.y, baseScale.z / ls.z);

        if (col != null) col.enabled = false;
        HUD.SetArtifactText("");
    }

    // VR: el Ascua va en una mano (o en la mochila del hobbit)
    public void AttachVR(Transform carrier, Vector3 localPos, float scaleMul)
    {
        if (IsGone) return;
        IsCarried = true;
        TimeLeft = loseTime;
        transform.SetParent(carrier, false);
        transform.localPosition = localPos;
        transform.localRotation = Quaternion.identity;
        Vector3 ls = carrier.lossyScale;
        transform.localScale = new Vector3(baseScale.x * scaleMul / ls.x, baseScale.y * scaleMul / ls.y, baseScale.z * scaleMul / ls.z);
        if (col != null) col.enabled = false;
        HUD.SetArtifactText("");
    }

    public void Drop(PlayerMovement who)
    {
        if (!IsCarried || IsGone) return;

        Vector3 pos = who.transform.position + who.transform.forward * 1.2f;
        Vector3 ground = pos;
        float best = float.MaxValue;
        foreach (var h in Physics.RaycastAll(pos + Vector3.up * 3f, Vector3.down, 12f, ~0, QueryTriggerInteraction.Ignore))
        {
            if (h.collider.GetComponentInParent<PlayerMovement>() != null) continue;
            if (h.collider.transform.IsChildOf(transform)) continue;
            if (h.distance < best) { best = h.distance; ground = h.point; }
        }

        transform.SetParent(null);
        transform.localScale = baseScale;
        transform.position = ground + Vector3.up * 0.4f;

        IsCarried = false;
        TimeLeft = loseTime;
        if (col != null) col.enabled = true;

        Sfx.Beep(300f, 0.2f);
        HUD.Say("Soltaste el artefacto. Tienes " + Format(loseTime) + " para recogerlo");
    }

    // Tocar el artefacto en el suelo para recogerlo
    public override void Interact(PlayerMovement who)
    {
        if (IsCarried || IsGone) return;
        AttachTo(who.transform);
        Sfx.Beep(880f, 0.15f);
        HUD.Say("Recogiste el artefacto");
    }

    void Update()
    {
        if (IsCarried || IsGone) return;

        TimeLeft -= Time.deltaTime;
        HUD.SetArtifactText("Artefacto en el suelo: " + Format(TimeLeft));

        if (TimeLeft <= 0f)
        {
            IsGone = true;
            StartCoroutine(Lose());
        }
    }

    IEnumerator Lose()
    {
        HUD.SetArtifactText("");
        HUD.Say("Perdiste el artefacto. Reiniciando...");
        Sfx.Beep(110f, 0.6f);
        foreach (var r in GetComponentsInChildren<Renderer>()) r.enabled = false;
        if (col != null) col.enabled = false;
        Inventory.Clear();

        yield return new WaitForSeconds(3f);
        SceneManager.LoadScene(SceneManager.GetActiveScene().name);
    }

    // Lanzar el artefacto al crater (lo usa LavaCrater)
    public void ThrowInto(Vector3 target)
    {
        if (IsGone) return;
        IsGone = true;
        IsCarried = false;
        HUD.SetArtifactText("");
        StartCoroutine(ThrowRoutine(target));
    }

    IEnumerator ThrowRoutine(Vector3 target)
    {
        transform.SetParent(null);
        transform.localScale = baseScale;
        Vector3 start = transform.position;

        Sfx.Beep(600f, 0.15f);
        float t = 0f;
        while (t < 1f)
        {
            t += Time.deltaTime / 1.6f;
            Vector3 p = Vector3.Lerp(start, target, t);
            p.y += Mathf.Sin(Mathf.Clamp01(t) * Mathf.PI) * 5f;
            transform.position = p;
            yield return null;
        }

        foreach (var r in GetComponentsInChildren<Renderer>()) r.enabled = false;
        Sfx.Beep(220f, 0.4f);
        yield return new WaitForSeconds(0.4f);
        Sfx.Beep(660f, 0.2f);
        yield return new WaitForSeconds(0.2f);
        Sfx.Beep(880f, 0.35f);
        HUD.Say("Mision cumplida: el artefacto cayo al volcan");
        EndingCinematic.Play();
    }
}
