using UnityEngine;

// Base de todo objeto que otro puede activar (puerta, etc.).
public abstract class Activatable : MonoBehaviour
{
    public abstract void Activate();
    public virtual void Deactivate() { }
}
