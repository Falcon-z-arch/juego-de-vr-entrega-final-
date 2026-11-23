using UnityEngine;
using UnityEngine.InputSystem;

// Una mano VR: lee posicion, rotacion y botones del control (Quest/Touch via OpenXR).
// Solo guarda el estado; la logica del juego esta en VRRig.
public class VRHand : MonoBehaviour
{
    public bool isLeft;

    public bool GripDown, GripUp, TriggerDown, TriggerUp, ButtonA, ButtonB;
    public bool Grip, Trigger;
    public Vector2 Stick;
    public Vector3 Velocity;            // velocidad de la mano en el espacio del rig (m/s)

    InputAction pos, rot, grip, trigger, stick, a, b;
    Vector3 lastLocal;
    GameObject pointer;

    public Vector3 Forward => transform.forward;

    public void Setup(bool left)
    {
        isLeft = left;
        string h = left ? "{LeftHand}" : "{RightHand}";

        pos = new InputAction("pos", InputActionType.Value, "<XRController>" + h + "/devicePosition");
        rot = new InputAction("rot", InputActionType.Value, "<XRController>" + h + "/deviceRotation");
        grip = new InputAction("grip", InputActionType.Button);
        grip.AddBinding("<XRController>" + h + "/{GripButton}");
        trigger = new InputAction("trigger", InputActionType.Button);
        trigger.AddBinding("<XRController>" + h + "/{TriggerButton}");
        stick = new InputAction("stick", InputActionType.Value, "<XRController>" + h + "/{Primary2DAxis}");
        a = new InputAction("a", InputActionType.Button, "<XRController>" + h + "/{PrimaryButton}");
        b = new InputAction("b", InputActionType.Button, "<XRController>" + h + "/{SecondaryButton}");

        foreach (var x in new[] { pos, rot, grip, trigger, stick, a, b }) x.Enable();

        // esfera visible = la mano
        var vis = GameObject.CreatePrimitive(PrimitiveType.Sphere);
        vis.name = "Mano";
        Destroy(vis.GetComponent<Collider>());
        vis.transform.SetParent(transform, false);
        vis.transform.localScale = Vector3.one * 0.09f;
        vis.GetComponent<Renderer>().material.color = left ? new Color(0.3f, 0.6f, 1f) : new Color(1f, 0.5f, 0.2f);

        // rayo de puntero (solo mano derecha)
        if (!left)
        {
            pointer = GameObject.CreatePrimitive(PrimitiveType.Cube);
            pointer.name = "Puntero";
            Destroy(pointer.GetComponent<Collider>());
            pointer.transform.SetParent(transform, false);
            pointer.transform.localScale = new Vector3(0.006f, 0.006f, 4f);
            pointer.transform.localPosition = new Vector3(0f, 0f, 2f);
            pointer.GetComponent<Renderer>().material.color = new Color(1f, 1f, 0.6f);
        }
    }

    void OnDestroy()
    {
        foreach (var x in new[] { pos, rot, grip, trigger, stick, a, b }) x?.Dispose();
    }

    // Lo llama VRRig cada frame (en espacio local del rig)
    public void Poll()
    {
        if (pos == null) return;

        Vector3 p = pos.ReadValue<Vector3>();
        Quaternion q = rot.ReadValue<Quaternion>();
        if (q.x == 0f && q.y == 0f && q.z == 0f && q.w == 0f) q = Quaternion.identity;

        transform.localPosition = p;
        transform.localRotation = q;

        float dt = Mathf.Max(Time.deltaTime, 0.0001f);
        Velocity = Vector3.Lerp(Velocity, (p - lastLocal) / dt, 0.5f);
        lastLocal = p;

        GripDown = grip.WasPressedThisFrame();
        GripUp = grip.WasReleasedThisFrame();
        TriggerDown = trigger.WasPressedThisFrame();
        TriggerUp = trigger.WasReleasedThisFrame();
        Grip = grip.IsPressed();
        Trigger = trigger.IsPressed();
        Stick = stick.ReadValue<Vector2>();
        ButtonA = a.WasPressedThisFrame();
        ButtonB = b.WasPressedThisFrame();
    }

    public void ShowPointer(bool on)
    {
        if (pointer != null && pointer.activeSelf != on) pointer.SetActive(on);
    }
}
