using UnityEngine;

// Interaccion 8: Puerta que se desliza hacia arriba al activarse (con sonido y mensaje).
public class Door : Activatable
{
    public Vector3 openOffset = new Vector3(0f, 3.5f, 0f);
    public float speed = 2f;
    public AudioClip sound;

    Vector3 closedPos;
    bool open;

    void Start() => closedPos = transform.localPosition;

    public override void Activate()
    {
        if (open) return;
        open = true;
        Sfx.Ping(sound, 330f);
        HUD.Say("La puerta se abre");
    }

    public override void Deactivate()
    {
        if (!open) return;
        open = false;
        Sfx.Ping(sound, 260f);
        HUD.Say("La puerta se cierra");
    }

    void Update()
    {
        Vector3 t = open ? closedPos + openOffset : closedPos;
        transform.localPosition = Vector3.MoveTowards(transform.localPosition, t, speed * Time.deltaTime);
    }
}
