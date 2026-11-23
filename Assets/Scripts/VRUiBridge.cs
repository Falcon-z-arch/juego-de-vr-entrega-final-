using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.UI;

// Menus en VR: los canvas "Screen Space Overlay" (tienda, altar, tutoriales, dialogo) NO se ven dentro del lente.
// Este componente los convierte a World Space delante de la cabeza y deja pulsar sus botones apuntando con el
// control derecho (o el izquierdo) y apretando el gatillo. Lo agrega VRRig al activarse el modo VR.
public class VRUiBridge : MonoBehaviour
{
    public Camera cam;
    public Transform head, hand;

    readonly HashSet<Canvas> done = new HashSet<Canvas>();
    InputAction rTrig, lTrig;
    Transform cursor, beam;
    float scanT;
    readonly List<RaycastResult> results = new List<RaycastResult>();

    void Start()
    {
        rTrig = new InputAction("rt", InputActionType.Button, "<XRController>{RightHand}/trigger");
        lTrig = new InputAction("lt", InputActionType.Button, "<XRController>{LeftHand}/trigger");
        rTrig.Enable(); lTrig.Enable();
        cursor = Make("CursorUI", PrimitiveType.Sphere, Vector3.one * 0.025f);
        beam = Make("RayoUI", PrimitiveType.Cube, new Vector3(0.004f, 0.004f, 1f));
        cursor.gameObject.SetActive(false); beam.gameObject.SetActive(false);
    }

    static Transform Make(string n, PrimitiveType t, Vector3 s)
    {
        var g = GameObject.CreatePrimitive(t);
        g.name = n; g.transform.localScale = s;
        Destroy(g.GetComponent<Collider>());
        var r = g.GetComponent<Renderer>();
        r.material.color = new Color(0.4f, 0.9f, 1f);
        r.material.EnableKeyword("_EMISSION"); r.material.SetColor("_EmissionColor", new Color(0.4f, 0.9f, 1f) * 2f);
        r.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
        return g.transform;
    }

    void OnDestroy() { if (cursor != null) Destroy(cursor.gameObject); if (beam != null) Destroy(beam.gameObject); }

    void Update()
    {
        if (cam == null || head == null) return;
        scanT -= Time.unscaledDeltaTime;
        if (scanT <= 0f) { scanT = 0.25f; Scan(); }
        Point();
    }

    void Scan()
    {
        foreach (var c in FindObjectsByType<Canvas>(FindObjectsSortMode.None))
        {
            if (c == null || c.renderMode != RenderMode.ScreenSpaceOverlay || done.Contains(c)) continue;
            done.Add(c);
            string n = c.name;
            bool lockHead = n.StartsWith("Cinematica") || n.StartsWith("Overlay");
            ToWorld(c, lockHead);
        }
    }

    void ToWorld(Canvas c, bool lockHead)
    {
        c.renderMode = RenderMode.WorldSpace;
        c.worldCamera = cam;
        var sc = c.GetComponent<CanvasScaler>(); if (sc != null) sc.enabled = false;
        if (c.GetComponent<GraphicRaycaster>() == null) c.gameObject.AddComponent<GraphicRaycaster>();
        var rt = (RectTransform)c.transform;
        rt.sizeDelta = new Vector2(1920f, 1080f);
        if (lockHead)
        {
            rt.SetParent(head, false);
            rt.localScale = Vector3.one * 0.0012f;
            rt.localPosition = new Vector3(0f, 0f, 1.5f);
            rt.localRotation = Quaternion.identity;
        }
        else
        {
            Vector3 f = head.forward; f.y = 0f;
            f = f.sqrMagnitude < 0.01f ? Vector3.forward : f.normalized;
            rt.localScale = Vector3.one * 0.0011f;
            rt.position = head.position + f * 1.7f;
            rt.rotation = Quaternion.LookRotation(f);
        }
    }

    // apunta con el control; el gatillo pulsa el boton bajo el cursor
    void Point()
    {
        bool shown = false;
        if (hand != null && EventSystem.current != null)
        {
            var ray = new Ray(hand.position, hand.forward);
            float bestD = 1e9f; Canvas bestC = null; Vector3 bestP = Vector3.zero;
            foreach (var c in FindObjectsByType<Canvas>(FindObjectsSortMode.None))
            {
                if (c == null || c.renderMode != RenderMode.WorldSpace || !c.gameObject.activeInHierarchy || c.GetComponent<GraphicRaycaster>() == null) continue;
                var rt = (RectTransform)c.transform;
                var plane = new Plane(-rt.forward, rt.position);
                if (!plane.Raycast(ray, out float d) || d <= 0f || d > 8f || d >= bestD) continue;
                Vector3 p = ray.GetPoint(d);
                Vector3 l = rt.InverseTransformPoint(p);
                if (Mathf.Abs(l.x) > rt.rect.width * 0.5f || Mathf.Abs(l.y) > rt.rect.height * 0.5f) continue;
                bestD = d; bestC = c; bestP = p;
            }
            if (bestC != null)
            {
                var ped = new PointerEventData(EventSystem.current) { position = cam.WorldToScreenPoint(bestP) };
                results.Clear();
                bestC.GetComponent<GraphicRaycaster>().Raycast(ped, results);
                Button btn = null;
                foreach (var r in results) { btn = r.gameObject.GetComponentInParent<Button>(); if (btn != null && btn.interactable) break; btn = null; }
                if (btn != null)
                {
                    shown = true;
                    cursor.position = bestP;
                    beam.position = (hand.position + bestP) * 0.5f;
                    beam.rotation = Quaternion.LookRotation(bestP - hand.position);
                    beam.localScale = new Vector3(0.004f, 0.004f, bestD);
                    if (rTrig.WasPressedThisFrame() || lTrig.WasPressedThisFrame())
                    {
                        ExecuteEvents.Execute(btn.gameObject, ped, ExecuteEvents.pointerClickHandler);
                        Sfx.Beep(700f, 0.06f);
                    }
                }
            }
        }
        cursor.gameObject.SetActive(shown); beam.gameObject.SetActive(shown);
    }
}
