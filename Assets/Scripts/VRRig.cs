using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;
using UnityEngine.XR;

// Modo VR (Quest / OpenXR). Si no hay casco conectado, este componente no hace nada y el juego sigue en modo plano.
// Mecanicas con las DOS manos:
//   - Escalada mano sobre mano: gatillo lateral (grip) sobre un agarre ClimbHold, jalas el cuerpo hacia arriba.
//   - Arco magico (Mago): ambos gatillos; la mano derecha tira de la cuerda, la izquierda apunta. Mas separacion = mas carga.
//   - Cubos: grip para agarrar y soltar (lanzarlos con el movimiento de la mano).
public class VRRig : MonoBehaviour
{
    public static bool Active;
    public static VRRig Instance;

    public PartyManager party;
    public AbilityController ability;
    public bool forceActive;            // para probar sin casco (las manos no se moveran)
    public float snapAngle = 30f;
    public float interactRange = 6f;

    VRHand left, right;
    Transform head;
    Camera cam;
    InputAction hmdPos, hmdRot;
    bool inited;

    PlayerMovement cur, lastChar;
    ClimbHold[] holds = new ClimbHold[2];
    Vector3[] anchors = new Vector3[2];
    PuzzleCube[] cubes = new PuzzleCube[2];
    WeaponItem[] weapons = new WeaponItem[2];
    int primary = -1;
    bool wasClimbing;
    Vector3 lastCharPos, charVel;

    bool bowing;
    float charge;
    float[] trigTime = new float[2];
    bool[] bowed = new bool[2];
    Transform bowString;
    float snapCooldown;
    Transform backpack;   // donde lleva el Ascua el hobbit

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    static void ResetStatics() { Active = false; Instance = null; }

    void Update()
    {
        if (!inited)
        {
            if (XRSettings.isDeviceActive || forceActive || PlayerPrefs.GetInt("modoVR", 0) == 1) Init();
            return;
        }

        cur = party.Current;
        if (cur != lastChar)
        {
            lastChar = cur;
            DropWeapons();
            lastCharPos = cur.transform.position;
            HideBody(cur);
        }
        cur.rotateBody = false;

        SyncRig();
        PollHead();
        left.Poll();
        right.Poll();

        PlaceArtifact();
        HandleClimb();
        HandleGrab();
        HandleRole();
        HandleButtons();

        SyncRig();

        float dt = Mathf.Max(Time.deltaTime, 0.0001f);
        charVel = Vector3.Lerp(charVel, (cur.transform.position - lastCharPos) / dt, 0.3f);
        lastCharPos = cur.transform.position;
    }

    // ---------- inicio ----------
    void Init()
    {
        inited = true;
        Active = true;
        Instance = this;

        cam = Camera.main;
        head = cam.transform;
        var follow = cam.GetComponent<CameraFollow>();
        if (follow != null) follow.enabled = false;
        head.SetParent(transform, false);

        hmdPos = new InputAction("hmdPos", InputActionType.Value, "<XRHMD>/centerEyePosition");
        hmdRot = new InputAction("hmdRot", InputActionType.Value, "<XRHMD>/centerEyeRotation");
        hmdPos.Enable();
        hmdRot.Enable();

        left = MakeHand(true);
        right = MakeHand(false);

        var s = GameObject.CreatePrimitive(PrimitiveType.Cube);
        s.name = "CuerdaArco";
        Destroy(s.GetComponent<Collider>());
        s.GetComponent<Renderer>().material.color = new Color(0.3f, 0.9f, 1f);
        s.transform.SetParent(transform, false);
        s.SetActive(false);
        bowString = s.transform;

        var bp = new GameObject("Mochila");
        bp.transform.SetParent(transform, false);
        bp.transform.localPosition = new Vector3(0f, 1.15f, -0.35f);
        backpack = bp.transform;

        SetupHud();
        var bridge = gameObject.AddComponent<VRUiBridge>();
        bridge.cam = cam; bridge.head = head; bridge.hand = right.transform;
        HUD.Say("VR activo. El Ascua va en tu mano izquierda (el hobbit la guarda en su mochila). Mago: gatillo = magia. Peleador: grip = arma.");
    }

    VRHand MakeHand(bool isLeft)
    {
        var go = new GameObject(isLeft ? "ManoIzq" : "ManoDer");
        go.transform.SetParent(transform, false);
        var h = go.AddComponent<VRHand>();
        h.Setup(isLeft);
        return h;
    }

    void SetupHud()
    {
        var canvas = FindFirstObjectByType<Canvas>();
        if (canvas == null) return;

        foreach (var b in canvas.GetComponentsInChildren<Button>(true)) b.gameObject.SetActive(false);
        foreach (var t in canvas.GetComponentsInChildren<Transform>(true))
        {
            string n = t.name.ToLower();
            if (n.Contains("joystick") || n == "mira") t.gameObject.SetActive(false);
        }

        canvas.renderMode = RenderMode.WorldSpace;
        canvas.worldCamera = cam;
        var rt = canvas.GetComponent<RectTransform>();
        rt.SetParent(head, false);
        rt.sizeDelta = new Vector2(1600f, 900f);
        rt.localScale = Vector3.one * 0.0007f;
        rt.localPosition = new Vector3(0f, -0.1f, 1.3f);
        rt.localRotation = Quaternion.identity;
    }

    void HideBody(PlayerMovement p)
    {
        foreach (var r in p.GetComponentsInChildren<Renderer>(true)) r.enabled = false;
    }

    // ---------- seguimiento ----------
    void SyncRig()
    {
        var cc = cur.Controller;
        float feet = (cc != null && cc.enabled) ? cc.bounds.min.y : cur.transform.position.y - 1f;
        transform.position = new Vector3(cur.transform.position.x, feet, cur.transform.position.z);
    }

    void PollHead()
    {
        Vector3 p = hmdPos.ReadValue<Vector3>();
        Quaternion q = hmdRot.ReadValue<Quaternion>();
        if (q.x == 0f && q.y == 0f && q.z == 0f && q.w == 0f) { q = Quaternion.identity; p = new Vector3(0f, 1.6f, 0f); }
        head.localPosition = p;
        head.localRotation = q;
    }

    VRHand Hand(int i) => i == 0 ? left : right;

    bool IsHobbit()
    {
        var r = cur != null ? cur.GetComponent<CharacterRole>() : null;
        return r != null && r.role == Role.Hobbit;
    }

    // la mano izquierda queda ocupada con el Ascua (salvo el hobbit, que la lleva en la mochila)
    bool LeftBusy => party != null && party.artifact != null && party.artifact.IsCarried && !party.artifact.IsGone && !IsHobbit();
    bool Busy(int i) => i == 0 && LeftBusy;

    void PlaceArtifact()
    {
        var art = party.artifact;
        if (art == null || !art.IsCarried || art.IsGone) return;
        Transform want = IsHobbit() ? backpack : left.transform;
        if (art.transform.parent == want) return;
        art.AttachVR(want, IsHobbit() ? Vector3.zero : new Vector3(0f, 0.04f, 0.1f), IsHobbit() ? 0.8f : 0.6f);
    }

    // ---------- escalada ----------
    void HandleClimb()
    {
        for (int i = 0; i < 2; i++)
        {
            var h = Hand(i);
            if (Busy(i)) continue;
            if (h.GripDown && cubes[i] == null && weapons[i] == null && PuzzleCube.HeldCube == null)
            {
                var hold = NearestHold(h.transform.position);
                if (hold != null)
                {
                    holds[i] = hold;
                    anchors[i] = h.transform.position;
                    primary = i;
                    Sfx.Beep(500f, 0.05f);
                }
            }
            if (h.GripUp && holds[i] != null) ReleaseHold(i);
        }

        if (primary >= 0 && holds[primary] == null)
        {
            primary = holds[1 - primary] != null ? 1 - primary : -1;
            if (primary >= 0) anchors[primary] = Hand(primary).transform.position;
        }

        bool climbing = primary >= 0;
        if (climbing)
        {
            cur.locked = true;
            Vector3 delta = anchors[primary] - Hand(primary).transform.position;
            cur.Controller.Move(delta);
            SyncRig();

            // si el agarre se aleja demasiado, te resbalas
            if ((anchors[primary] - Hand(primary).transform.position).magnitude > 0.35f)
            {
                int i = primary;
                ReleaseHold(i);
                HUD.Say("¡Resbalaste!");
            }
        }
        else if (wasClimbing)
        {
            cur.locked = false;
        }
        wasClimbing = climbing;
    }

    ClimbHold NearestHold(Vector3 pos)
    {
        var cols = Physics.OverlapSphere(pos, 0.3f, ~0, QueryTriggerInteraction.Collide);
        ClimbHold best = null;
        float bd = 99f;
        foreach (var c in cols)
        {
            var h = c.GetComponentInParent<ClimbHold>();
            if (h == null) continue;
            float d = Vector3.Distance(pos, c.ClosestPoint(pos));
            if (d < bd) { bd = d; best = h; }
        }
        return best;
    }

    void ReleaseHold(int i)
    {
        var h = holds[i];
        holds[i] = null;
        if (holds[0] != null || holds[1] != null) return;

        primary = -1;
        cur.locked = false;
        wasClimbing = false;

        // al llegar al borde de una plataforma, te sube
        if (h != null && h.mantle && head.position.y > h.transform.position.y - 0.25f)
        {
            cur.Teleport(h.MantlePoint);
            HUD.Say("¡Arriba!");
            Sfx.Beep(800f, 0.1f);
            return;
        }
        cur.Launch(new Vector3(charVel.x, Mathf.Clamp(charVel.y, -6f, 9f), charVel.z));
    }

    // ---------- cubos ----------
    void HandleGrab()
    {
        for (int i = 0; i < 2; i++)
        {
            var h = Hand(i);
            if (Busy(i)) continue;

            // Peleador: armas
            if (IsFighter() && h.GripDown && holds[i] == null && cubes[i] == null && weapons[i] == null)
            {
                var wc = Physics.OverlapSphere(h.transform.position, 0.45f, ~0, QueryTriggerInteraction.Ignore);
                foreach (var c in wc)
                {
                    var w = c.GetComponentInParent<WeaponItem>();
                    if (w == null || w.Held) continue;
                    w.Hold(h.transform);
                    weapons[i] = w;
                    Sfx.Beep(450f, 0.06f);
                    break;
                }
            }
            if (weapons[i] != null && (h.GripUp || !IsFighter()))
            {
                weapons[i].Release(transform.TransformVector(h.Velocity) * 1.2f + charVel * 0.5f);
                weapons[i] = null;
            }

            if (h.GripDown && holds[i] == null && cubes[i] == null && weapons[i] == null && PuzzleCube.HeldCube == null)
            {
                var cols = Physics.OverlapSphere(h.transform.position, 0.35f, ~0, QueryTriggerInteraction.Ignore);
                foreach (var c in cols)
                {
                    var cube = c.GetComponentInParent<PuzzleCube>();
                    if (cube == null || cube.Held) continue;
                    cube.Hold(h.transform);
                    cubes[i] = cube;
                    Sfx.Beep(600f, 0.06f);
                    break;
                }
            }
            if (h.GripUp && cubes[i] != null)
            {
                Vector3 v = transform.TransformVector(h.Velocity) * 1.3f + charVel * 0.5f;
                cubes[i].Release(v);
                cubes[i] = null;
            }
            // si el cubo se solto por otro motivo (reinicio), limpiar
            if (cubes[i] != null && !cubes[i].Held) cubes[i] = null;
        }
    }

    bool IsFighter()
    {
        var r = cur != null ? cur.GetComponent<CharacterRole>() : null;
        return r != null && r.role == Role.Fighter;
    }

    void DropWeapons()
    {
        for (int i = 0; i < 2; i++)
            if (weapons[i] != null) { weapons[i].Release(Vector3.zero); weapons[i] = null; }
    }

    // ---------- habilidades por personaje ----------
    void HandleRole()
    {
        var role = cur.GetComponent<CharacterRole>();
        if (role == null) return;

        if (role.role == Role.Engineer)
        {
            for (int i = 0; i < 2; i++)
            {
                var h = Hand(i);
                if (Busy(i)) continue;
                if (h.TriggerDown && ability != null) ability.GrappleFrom(cur, h.transform.position, h.Forward);
            }
        }
        else if (role.role == Role.Mage)
        {
            HandleBow();
        }

        bowString.gameObject.SetActive(bowing && role.role == Role.Mage);
    }

    void HandleBow()
    {
        float dt = Time.deltaTime;
        for (int i = 0; i < 2; i++)
        {
            var h = Hand(i);
            if (h.TriggerDown) { trigTime[i] = 0f; bowed[i] = false; }
            if (h.Trigger) trigTime[i] += dt;
        }

        bool both = !LeftBusy && left.Trigger && right.Trigger;
        if (both)
        {
            bowing = true;
            bowed[0] = bowed[1] = true;

            Vector3 a = left.transform.position, b = right.transform.position;
            float d = Vector3.Distance(a, b);
            charge = Mathf.InverseLerp(0.12f, 0.6f, d);

            bowString.position = (a + b) * 0.5f;
            bowString.rotation = Quaternion.LookRotation((a - b).normalized);
            bowString.localScale = new Vector3(0.01f + charge * 0.02f, 0.01f + charge * 0.02f, d);
        }
        else if (bowing)
        {
            bowing = false;
            if (charge > 0.15f)
            {
                Vector3 origin = left.transform.position;
                Vector3 dir = (left.transform.position - right.transform.position).normalized;
                int power = charge > 0.8f ? 2 : 1;
                MagicShooter.Fire(origin, dir, 26f + charge * 22f, power);
                if (power > 1) HUD.Say("¡Disparo cargado!");
            }
            charge = 0f;
        }

        // disparo rapido: toque corto de un gatillo
        for (int i = 0; i < 2; i++)
        {
            var h = Hand(i);
            if (Busy(i)) continue;
            if (h.TriggerUp && !bowed[i] && trigTime[i] < 0.3f)
            {
                MagicShooter.Fire(h.transform.position + h.Forward * 0.1f, h.Forward);
            }
        }
    }

    // ---------- botones y giro ----------
    void HandleButtons()
    {
        bool climbing = primary >= 0;

        if (right.ButtonA && !climbing) party.Jump();                 // A: saltar
        if (left.ButtonA) party.SwitchNext();                         // X: cambiar de personaje
        if (left.ButtonB)                                             // Y: agacharse (hobbit)
        {
            var role = cur.GetComponent<CharacterRole>();
            if (role != null && role.role == Role.Hobbit) role.ToggleCrouch();
            else HUD.Say("Y solo sirve para el hobbit");
        }
        if (right.ButtonB) InteractRay();                             // B: interactuar apuntando

        // giro por pasos con el joystick derecho
        snapCooldown -= Time.deltaTime;
        if (snapCooldown <= 0f && !climbing && Mathf.Abs(right.Stick.x) > 0.7f)
        {
            transform.Rotate(0f, Mathf.Sign(right.Stick.x) * snapAngle, 0f);
            snapCooldown = 0.25f;
        }
    }

    void InteractRay()
    {
        var ray = new Ray(right.transform.position, right.Forward);
        var hits = Physics.RaycastAll(ray, 30f, ~0, QueryTriggerInteraction.Ignore);
        System.Array.Sort(hits, (a, b) => a.distance.CompareTo(b.distance));

        foreach (var h in hits)
        {
            if (h.collider.GetComponentInParent<PlayerMovement>() != null) continue;
            if (h.collider.GetComponentInParent<PuzzleCube>() != null) return;   // los cubos se agarran con grip

            var it = h.collider.GetComponentInParent<Interactable>();
            if (it == null) return;

            float limit = it.interactDistance > 0f ? it.interactDistance : interactRange;
            if (Vector3.Distance(cur.transform.position, it.transform.position) > limit)
            {
                HUD.Say("Acercate mas a: " + it.prompt);
                return;
            }
            it.Interact(cur);
            return;
        }
    }
}
