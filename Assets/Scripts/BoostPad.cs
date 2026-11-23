using UnityEngine;

// Respiradero de lava: da un gran impulso hacia arriba para subir al siguiente nivel del volcan.
// Empieza apagado (gris). Un puzzle lo activa (palanca, cristal, maquina, placa) y se enciende (naranja).
// Necesita un Collider con "Is Trigger" y un Rigidbody kinematico en el objeto.
public class BoostPad : Activatable
{
    public Vector3 horizontalDirection = Vector3.forward;   // hacia donde empuja (hacia el centro del volcan)
    public float upSpeed = 18f;
    public float horizontalSpeed = 4.5f;
    public Renderer pad;
    public Light glow;
    public Color offColor = new Color(0.25f, 0.25f, 0.25f);
    public Color onColor = new Color(1f, 0.45f, 0.05f);

    bool active;
    float cooldown;

    void Start() => Paint();

    public override void Activate()
    {
        if (active) return;
        active = true;
        Paint();
        Sfx.Beep(500f, 0.15f);
        HUD.Say("El respiradero se enciende: pisalo para salir disparado");
    }

    void Paint()
    {
        if (pad != null)
        {
            var m = pad.material;
            m.color = active ? onColor : offColor;
            if (m.HasProperty("_BaseColor")) m.SetColor("_BaseColor", active ? onColor : offColor);
            m.EnableKeyword("_EMISSION");
            m.SetColor("_EmissionColor", active ? onColor * 2f : Color.black);
        }
        if (glow != null) glow.enabled = active;
    }

    void OnTriggerStay(Collider other) => TryLaunch(other);
    void OnTriggerEnter(Collider other) => TryLaunch(other);

    void TryLaunch(Collider other)
    {
        if (!active || Time.time < cooldown) return;
        var p = other.GetComponentInParent<PlayerMovement>();
        if (p == null || !p.controlled) return;

        cooldown = Time.time + 1.5f;
        Vector3 h = horizontalDirection;
        h.y = 0f;
        h = h.normalized * horizontalSpeed;
        p.Launch(new Vector3(h.x, upSpeed, h.z));
        Sfx.Beep(300f, 0.25f);
        HUD.Say("Impulso!");
    }
}
