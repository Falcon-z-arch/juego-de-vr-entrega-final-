using UnityEngine;

// Baliza de checkpoint: una recompensa de puzzle. Al activarse (por una placa) guarda tu punto de reaparicion.
public class CheckpointBeacon : Activatable
{
    public Renderer lamp;
    public Light glow;
    public Color onColor = new Color(1f, 0.9f, 0.2f);

    bool on;

    void Start() => Paint();

    public override void Activate()
    {
        if (on) return;
        on = true;
        Checkpoint.SetPoint(transform.position);
        Paint();
        Sfx.Beep(880f, 0.2f);
        HUD.Say("Checkpoint desbloqueado");
    }

    void Paint()
    {
        if (lamp != null)
        {
            var m = lamp.material;
            Color c = on ? onColor : new Color(0.3f, 0.3f, 0.3f);
            m.color = c;
            if (m.HasProperty("_BaseColor")) m.SetColor("_BaseColor", c);
            m.EnableKeyword("_EMISSION");
            m.SetColor("_EmissionColor", on ? c * 2f : Color.black);
        }
        if (glow != null) glow.enabled = on;
    }
}
