using UnityEngine;

// Placa de presion (estilo Portal): esta activa mientras haya un cubo apoyado encima
// (o el jugador, si cubeOnly es falso). Necesita Collider "Is Trigger" y Rigidbody kinematico.
public class WeightPlate : MonoBehaviour
{
    public Activatable[] targets;
    public Renderer plate;
    public bool cubeOnly = true;
    public Color offColor = new Color(0.45f, 0.45f, 0.5f);
    public Color onColor = new Color(0.2f, 1f, 0.4f);

    bool seen;
    bool pressed;

    void Start() => Paint(false);

    void OnTriggerStay(Collider other)
    {
        if (Valid(other)) seen = true;
    }

    bool Valid(Collider o)
    {
        var cube = o.GetComponentInParent<PuzzleCube>();
        if (cube != null) return !cube.Held;
        return !cubeOnly && o.GetComponentInParent<PlayerMovement>() != null;
    }

    void FixedUpdate()
    {
        bool now = seen;
        seen = false;
        if (now == pressed) return;

        pressed = now;
        Paint(now);
        Sfx.Beep(now ? 700f : 350f, 0.12f);
        HUD.Say(now ? "Placa activada" : "Placa liberada");

        if (targets == null) return;
        foreach (var t in targets)
        {
            if (t == null) continue;
            if (now) t.Activate(); else t.Deactivate();
        }
    }

    void Paint(bool on)
    {
        if (plate == null) return;
        var m = plate.material;
        Color c = on ? onColor : offColor;
        m.color = c;
        if (m.HasProperty("_BaseColor")) m.SetColor("_BaseColor", c);
        m.EnableKeyword("_EMISSION");
        m.SetColor("_EmissionColor", on ? c * 1.5f : Color.black);
    }
}
