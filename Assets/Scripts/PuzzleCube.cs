using UnityEngine;

// Cubo de puzzle (estilo Portal): se agarra, se lanza y se deja sobre una placa para activarla.
// VR: lo agarran las manos (VRHand). Modo plano: tocarlo lo levanta; cualquier toque despues lo lanza.
// Si cae al vacio o a un nivel mucho mas bajo, vuelve a su posicion inicial.
[RequireComponent(typeof(Rigidbody))]
public class PuzzleCube : Interactable
{
    public static PuzzleCube HeldCube;

    public bool Held { get; private set; }

    Rigidbody rb;
    Collider col;
    Vector3 startPos;
    Quaternion startRot;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    static void ResetStatics() => HeldCube = null;

    void Awake()
    {
        prompt = "Cubo";
        rb = GetComponent<Rigidbody>();
        col = GetComponent<Collider>();
        rb.mass = 5f;
        rb.sleepThreshold = 0f;   // nunca "duerme": asi la placa siempre lo detecta
        rb.collisionDetectionMode = CollisionDetectionMode.ContinuousDynamic;
        startPos = transform.position;
        startRot = transform.rotation;
    }

    // Modo plano: tocar el cubo lo levanta
    public override void Interact(PlayerMovement who)
    {
        if (Held || HeldCube != null) return;
        Hold(Camera.main.transform, new Vector3(0.1f, -0.3f, 1.5f));
        Sfx.Beep(600f, 0.08f);
        HUD.Say("Cubo en la mano. Toca la pantalla para lanzarlo");
    }

    // Modo plano: lo lanza hacia donde miras
    public void ThrowFlat()
    {
        var cam = Camera.main.transform;
        Release(cam.forward * 9f + Vector3.up * 2.5f);
        Sfx.Beep(400f, 0.1f);
    }

    public void Hold(Transform holder, Vector3? localPos = null)
    {
        if (HeldCube != null && HeldCube != this) HeldCube.Release(Vector3.zero);
        Held = true;
        HeldCube = this;
        rb.isKinematic = true;
        col.enabled = false;
        transform.SetParent(holder, false);
        transform.localPosition = localPos ?? Vector3.zero;
        transform.localRotation = Quaternion.identity;
    }

    public void Release(Vector3 velocity)
    {
        if (!Held) return;
        Held = false;
        if (HeldCube == this) HeldCube = null;
        transform.SetParent(null);
        col.enabled = true;
        rb.isKinematic = false;
        rb.linearVelocity = velocity;
    }

    void Update()
    {
        if (!Held && transform.position.y < startPos.y - 8f) ResetToStart();
    }

    public void ResetToStart()
    {
        if (Held) Release(Vector3.zero);
        rb.isKinematic = true;
        transform.SetPositionAndRotation(startPos, startRot);
        rb.isKinematic = false;
        rb.linearVelocity = Vector3.zero;
        rb.angularVelocity = Vector3.zero;
        HUD.Say("El cubo volvio a su lugar");
    }
}
