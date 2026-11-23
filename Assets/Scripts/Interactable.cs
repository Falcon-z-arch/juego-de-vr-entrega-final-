using UnityEngine;

// Base de todo objeto que se activa tocando la pantalla.
// "targets" = objetos que se activan despues (asi se arma la cadena logica desde el Inspector).
public abstract class Interactable : MonoBehaviour
{
    public string prompt = "Interactuar";
    public AudioClip sound;               // opcional
    public Activatable[] targets;
    public float interactDistance = 0f;   // 0 = usa la distancia general de TapInteractor

    public abstract void Interact(PlayerMovement who);

    protected void SetTargets(bool on)
    {
        if (targets == null) return;
        foreach (var t in targets)
        {
            if (t == null) continue;
            if (on) t.Activate(); else t.Deactivate();
        }
    }
}
