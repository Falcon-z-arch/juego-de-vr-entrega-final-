using UnityEngine;

// Interaccion 9: Interruptor de luz / antorcha. Enciende y apaga una luz y el brillo del objeto.
public class LightSwitch : Interactable
{
    public Light lamp;
    public Renderer bulb;
    public Color color = new Color(1f, 0.8f, 0.4f);
    public bool startOn;

    bool on;

    void Start() => Apply(startOn);

    public override void Interact(PlayerMovement who)
    {
        Apply(!on);
        Sfx.Ping(sound, on ? 1000f : 600f);
        HUD.Say(on ? "Luz encendida" : "Luz apagada");
        SetTargets(on);
    }

    void Apply(bool value)
    {
        on = value;
        if (lamp != null) lamp.enabled = on;
        if (bulb != null)
        {
            var m = bulb.material;
            m.EnableKeyword("_EMISSION");
            m.SetColor("_EmissionColor", on ? color * 2f : Color.black);
        }
    }
}
