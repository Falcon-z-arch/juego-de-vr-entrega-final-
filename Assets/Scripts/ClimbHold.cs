using UnityEngine;

// Agarre de escalada (VR): las manos se aferran a este objeto y el cuerpo se mueve al jalar.
// Si "mantle" es true, al soltarlo cerca del borde superior te sube a la plataforma.
public class ClimbHold : MonoBehaviour
{
    public bool mantle = false;
    public Vector3 mantleOffset = new Vector3(0f, 1.2f, 0f);

    public Vector3 MantlePoint => transform.position + mantleOffset;
}
