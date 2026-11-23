using UnityEngine;
using UnityEngine.InputSystem;

// Movimiento en primera persona: joystick tactil o WASD (relativo a hacia donde mira la camara),
// salto (boton o espacio) e impulsos externos (respiraderos de lava, golpes de roca).
[RequireComponent(typeof(CharacterController))]
public class PlayerMovement : MonoBehaviour
{
    public InputActionReference move;     // opcional
    public float speed = 5f;
    public float gravity = -20f;
    public float jumpHeight = 1.5f;
    public float airControl = 0.6f;

    [HideInInspector] public bool controlled;
    [HideInInspector] public bool locked;   // true mientras el gancho lo arrastra o escalas
    [HideInInspector] public bool rotateBody = true;   // en VR se desactiva
    [HideInInspector] public float speedMul = 1f;      // lo modifica ArtifactBurden (lento / impulso de relevo)
    public float extraDecay = 1.2f;                    // cuanto se frena en el aire el impulso horizontal

    float noGroundReset;                               // durante un dash no se borra el impulso al tocar el suelo

    CharacterController cc;
    InputAction action;
    bool ownsAction;
    bool jumpQueued;
    float vy;
    Vector3 extra;                          // velocidad horizontal externa

    public CharacterController Controller => cc;

    void Awake()
    {
        cc = GetComponent<CharacterController>();

        if (move != null)
        {
            action = move.action;
        }
        else
        {
            action = new InputAction("Move", InputActionType.Value);
            action.AddBinding("<Gamepad>/leftStick");
            action.AddBinding("<XRController>{LeftHand}/{Primary2DAxis}");   // VR: joystick de la mano izquierda
            action.AddCompositeBinding("2DVector")
                .With("Up", "<Keyboard>/w")
                .With("Down", "<Keyboard>/s")
                .With("Left", "<Keyboard>/a")
                .With("Right", "<Keyboard>/d");
            ownsAction = true;
        }
    }

    void OnEnable() => action?.Enable();

    void OnDisable()
    {
        if (ownsAction) action?.Disable();
    }

    void OnDestroy()
    {
        if (ownsAction) action?.Dispose();
    }

    // Boton "Saltar" (via PartyManager.Jump)
    public void Jump()
    {
        if (controlled) jumpQueued = true;
    }

    // Lanza al personaje: velocity.y es vertical, x/z horizontal. Lo usan BoostPad y las rocas.
    public void Launch(Vector3 velocity)
    {
        vy = velocity.y;
        extra = new Vector3(velocity.x, 0f, velocity.z);
    }

    // Dash del gancho: velocidad completa (x,y,z) que no se borra al rozar el suelo durante "hold" segundos
    public void Dash(Vector3 velocity, float hold)
    {
        vy = velocity.y;
        extra = new Vector3(velocity.x, 0f, velocity.z);
        noGroundReset = Time.time + hold;
    }

    void Update()
    {
        if (locked || !cc.enabled) return;

        if (controlled && Keyboard.current != null && Keyboard.current.spaceKey.wasPressedThisFrame)
            jumpQueued = true;

        bool grounded = cc.isGrounded;
        Vector3 dir = Vector3.zero;

        if (controlled && action != null)
        {
            Vector2 input = action.ReadValue<Vector2>();
            Transform cam = Camera.main.transform;
            Vector3 fwd = Vector3.ProjectOnPlane(cam.forward, Vector3.up).normalized;
            Vector3 right = Vector3.ProjectOnPlane(cam.right, Vector3.up).normalized;
            dir = (fwd * input.y + right * input.x) * speed * speedMul;
            if (!grounded) dir *= airControl;
            if (rotateBody && fwd.sqrMagnitude > 0.01f) transform.rotation = Quaternion.LookRotation(fwd);
        }

        if (grounded && vy <= 0f && Time.time > noGroundReset)
        {
            vy = -2f;
            extra = Vector3.zero;
        }
        else
        {
            extra = Vector3.MoveTowards(extra, Vector3.zero, extraDecay * Time.deltaTime);
        }

        if (jumpQueued && grounded)
        {
            vy = Mathf.Sqrt(jumpHeight * RunState.JumpMul * -2f * gravity);
            Sfx.Beep(520f, 0.08f);
        }
        jumpQueued = false;

        vy += gravity * Time.deltaTime;
        Vector3 velocity = dir + extra;
        velocity.y = vy;
        cc.Move(velocity * Time.deltaTime);
    }

    public void Teleport(Vector3 pos)
    {
        cc.enabled = false;
        transform.position = pos;
        cc.enabled = true;
        vy = 0f;
        extra = Vector3.zero;
    }
}
