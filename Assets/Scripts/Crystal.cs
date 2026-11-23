using UnityEngine;

// Interaccion 5: Cristal que cambia de estado (color + brillo + sonido) al recibir un proyectil magico.
// Necesita un Collider normal (no trigger).
public class Crystal : MonoBehaviour
{
    public Activatable[] targets;
    public Color hitColor = new Color(0.3f, 0.9f, 1f);
    public Light glow;                    // opcional
    public AudioClip sound;               // opcional

    Renderer rend;
    bool hit;
    public bool IsHit => hit;

    void Awake()
    {
        rend = GetComponentInChildren<Renderer>();
        if (glow != null) glow.enabled = false;
    }

    public void Hit()
    {
        if (hit) return;
        hit = true;

        if (rend != null)
        {
            rend.material.color = hitColor;
            rend.material.EnableKeyword("_EMISSION");
            rend.material.SetColor("_EmissionColor", hitColor * 2f);
        }
        if (glow != null) glow.enabled = true;

        Sfx.Ping(sound, 1200f);
        HUD.Say("El cristal resuena");

        if (targets == null) return;
        foreach (var t in targets)
            if (t != null) t.Activate();
    }
}
