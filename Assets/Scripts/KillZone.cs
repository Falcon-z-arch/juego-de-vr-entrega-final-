using UnityEngine;

// Zona de caida: si el personaje la toca, reaparece en el ultimo checkpoint.
// Poner un cubo grande bajo la zona de pruebas con Collider "Is Trigger".
public class KillZone : MonoBehaviour
{
    void OnTriggerEnter(Collider other)
    {
        var p = other.GetComponentInParent<PlayerMovement>();
        if (p == null) return;
        Vector3 pos = p.transform.position;
        Debug.Log("KillZone '" + name + "' tocada en x=" + pos.x.ToString("0.0") + " y=" + pos.y.ToString("0.0") + " z=" + pos.z.ToString("0.0"));
        HUD.Say("Caiste (" + name + ") en x=" + Mathf.RoundToInt(pos.x) + " z=" + Mathf.RoundToInt(pos.z));
        Sfx.Beep(150f, 0.3f);
        Checkpoint.Respawn(p);
    }
}
