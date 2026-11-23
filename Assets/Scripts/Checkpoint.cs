using UnityEngine;

// Interaccion 13: Checkpoint. Guarda el punto de reaparicion. Necesita un Collider con "Is Trigger".
public class Checkpoint : MonoBehaviour
{
    static Vector3 last;
    static bool hasStart;

    public Renderer flag;
    public Color activeColor = Color.yellow;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    static void ResetStatics() => hasStart = false;

    public static void SetStart(Vector3 p)
    {
        if (!hasStart) { last = p; hasStart = true; }
    }

    public static void SetPoint(Vector3 p)
    {
        last = p;
        hasStart = true;
    }

    public static void Respawn(PlayerMovement p)
    {
        p.Teleport(last + Vector3.up * 0.5f);
    }

    void OnTriggerEnter(Collider other)
    {
        if (other.GetComponentInParent<PlayerMovement>() == null) return;
        last = transform.position;
        hasStart = true;
        if (flag != null) flag.material.color = activeColor;
        Sfx.Beep(700f, 0.2f);
        HUD.Say("Checkpoint guardado");
    }
}
