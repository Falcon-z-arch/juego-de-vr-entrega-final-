using UnityEngine;

// Interaccion 7: Palanca. Gira el mango (animacion) y activa/desactiva sus targets.
public class Lever : Interactable
{
    public Transform handle;
    public float angleOn = -45f;
    public float angleOff = 45f;
    public float speed = 8f;

    bool on;
    Quaternion target;

    void Start()
    {
        target = Quaternion.Euler(angleOff, 0f, 0f);
        if (handle != null) handle.localRotation = target;
    }

    public override void Interact(PlayerMovement who)
    {
        on = !on;
        target = Quaternion.Euler(on ? angleOn : angleOff, 0f, 0f);
        Sfx.Ping(sound, on ? 880f : 440f);
        HUD.Say(on ? "Palanca activada" : "Palanca desactivada");
        SetTargets(on);
    }

    void Update()
    {
        if (handle != null)
            handle.localRotation = Quaternion.Slerp(handle.localRotation, target, speed * Time.deltaTime);
    }
}
