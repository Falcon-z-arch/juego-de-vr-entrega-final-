using System.Collections;
using UnityEngine;

// Boton "Habilidad" en primera persona: hace algo distinto segun el personaje.
// Hobbit = agacharse, Mago = disparo magico hacia la mira, Ingeniero = gancho LIBRE (dash hacia cualquier superficie).
public class AbilityController : MonoBehaviour
{
    public PartyManager party;
    public float grappleRange = 28f;     // alcance del gancho libre
    public float hookSpeed = 28f;        // velocidad del dash
    public float hookCooldown = 0.6f;

    float nextHook;

    // Conectar al boton "Habilidad" de la UI
    public void UseAbility()
    {
        var who = party.Current;
        var r = who.GetComponent<CharacterRole>();
        if (r == null)
        {
            HUD.Say("Este personaje no tiene CharacterRole");
            return;
        }

        Transform cam = Camera.main.transform;
        switch (r.role)
        {
            case Role.Hobbit:
                r.ToggleCrouch();
                break;
            case Role.Mage:
                MagicShooter.Fire(cam.position + cam.forward * 0.8f - cam.up * 0.15f, cam.forward);
                break;
            case Role.Engineer:
                GrappleFrom(who, cam.position, cam.forward);
                break;
        }
    }

    // Gancho: si miras casi directo a un punto turquesa, te lleva a su plataforma (ayuda para subir torres).
    // Si no, se engancha a CUALQUIER superficie a la que apuntes y te lanza hacia ella como un dash.
    // Tambien lo usa la mano VR.
    public void GrappleFrom(PlayerMovement who, Vector3 origin, Vector3 forward)
    {
        if (Time.time < nextHook || who.locked) return;

        // 1) punto de enganche con aterrizaje (solo si lo miras con precision)
        GrapplePoint best = null;
        float bestDot = 0.93f;
        foreach (var g in FindObjectsByType<GrapplePoint>(FindObjectsSortMode.None))
        {
            Vector3 to = g.transform.position - origin;
            float d = to.magnitude;
            if (d > grappleRange || d < 0.5f) continue;
            float dot = Vector3.Dot(forward, to / d);
            if (dot > bestDot) { bestDot = dot; best = g; }
        }
        if (best != null)
        {
            nextHook = Time.time + hookCooldown * RunState.HookCooldownMul;
            best.Pull(who);
            return;
        }

        // 2) gancho libre: la primera superficie solida que toque el rayo
        var hits = Physics.RaycastAll(origin, forward, grappleRange, ~0, QueryTriggerInteraction.Ignore);
        System.Array.Sort(hits, (a, b) => a.distance.CompareTo(b.distance));
        foreach (var h in hits)
        {
            if (h.collider.GetComponentInParent<PlayerMovement>() != null) continue;
            if (h.collider.GetComponentInParent<FallingRock>() != null) continue;
            if (h.collider.GetComponentInParent<PuzzleCube>() != null) continue;
            if (h.collider.name == "Trozo") continue;

            nextHook = Time.time + hookCooldown * RunState.HookCooldownMul;
            StartCoroutine(HookDash(who, h.point + h.normal * 0.4f, h.point));
            return;
        }

        HUD.Say("El gancho no alcanzo nada (maximo " + (int)grappleRange + " m)");
        Sfx.Beep(200f, 0.15f);
    }

    // Dash: te jala hacia el punto y al llegar conservas el impulso (se puede encadenar)
    IEnumerator HookDash(PlayerMovement who, Vector3 target, Vector3 anchor)
    {
        var rope = GameObject.CreatePrimitive(PrimitiveType.Cube);
        Destroy(rope.GetComponent<Collider>());
        rope.GetComponent<Renderer>().material.color = new Color(0.2f, 0.9f, 0.9f);
        Sfx.Beep(520f, 0.1f);

        float t = 0f;
        Vector3 vel = Vector3.zero;
        while (t < 0.75f)
        {
            Vector3 body = who.transform.position + Vector3.up * 0.4f;
            Vector3 to = target - body;
            float dist = to.magnitude;
            if (dist < 1.8f) break;

            // acelera rapido hasta hookSpeed
            vel = Vector3.MoveTowards(vel, to / dist * hookSpeed, hookSpeed * 6f * Time.deltaTime);
            who.Dash(vel, 0.15f);

            Vector3 a = who.transform.position + Vector3.up * 0.9f;
            rope.transform.position = (a + anchor) * 0.5f;
            rope.transform.LookAt(anchor);
            rope.transform.localScale = new Vector3(0.04f, 0.04f, Vector3.Distance(a, anchor));

            t += Time.deltaTime;
            yield return null;
        }
        Destroy(rope);

        // al soltar: conservas parte del impulso y un saltito para trepar bordes
        Vector3 keep = vel * 0.6f;
        if (anchor.y > who.transform.position.y + 0.5f) keep.y = Mathf.Max(keep.y, 7f);
        who.Dash(keep, 0.25f);
    }
}
