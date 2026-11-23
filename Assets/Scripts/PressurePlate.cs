using UnityEngine;

// Interaccion 10: Placa de presion. Se activa mientras un personaje esta encima (cambia de color).
// Necesita un Collider con "Is Trigger" activado.
public class PressurePlate : MonoBehaviour
{
    public Activatable[] targets;
    public Renderer plate;
    public Color offColor = Color.gray;
    public Color onColor = Color.green;
    public bool stayPressed = false;

    int count;
    bool pressed;

    void Start() => Paint(false);

    void OnTriggerEnter(Collider other)
    {
        if (other.GetComponentInParent<PlayerMovement>() == null) return;
        count++;
        if (!pressed) Set(true);
    }

    void OnTriggerExit(Collider other)
    {
        if (other.GetComponentInParent<PlayerMovement>() == null) return;
        count = Mathf.Max(0, count - 1);
        if (count == 0 && pressed && !stayPressed) Set(false);
    }

    void Set(bool value)
    {
        pressed = value;
        Paint(value);
        Sfx.Beep(value ? 700f : 350f, 0.12f);
        HUD.Say(value ? "Placa presionada" : "Placa liberada");

        if (targets == null) return;
        foreach (var t in targets)
        {
            if (t == null) continue;
            if (value) t.Activate(); else t.Deactivate();
        }
    }

    void Paint(bool value)
    {
        if (plate != null) plate.material.color = value ? onColor : offColor;
    }
}
