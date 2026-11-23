using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;

// Camara en PRIMERA PERSONA (conserva el nombre CameraFollow por compatibilidad con la escena).
//  PC:      mantener clic derecho y mover el mouse para mirar
//  Celular: arrastrar un dedo por la mitad derecha de la pantalla para mirar
// Va a la altura de los ojos del personaje controlado. La cabeza siempre se oculta; el cuerpo aparece al mirar hacia abajo.
public class CameraFollow : MonoBehaviour
{
    public Transform target;
    public float yaw = 90f;
    public float pitch = 0f;
    public float mouseSensitivity = 0.12f;
    public float touchSensitivity = 0.18f;
    public float eyeFactor = 0.7f;        // altura de los ojos = factor * escala vertical del personaje

    [HideInInspector] public Vector3 offset;   // campo antiguo, sin uso

    Transform lastTarget;
    readonly List<Renderer> alwaysHidden = new List<Renderer>();
    readonly List<Renderer> bodyParts = new List<Renderer>();
    bool bodyShown;
    int lastAccN;
    float eye = -1f;

    void LateUpdate()
    {
        if (target == null) return;

        var dis = target.Find("Disenio");
        int accN = dis != null ? dis.childCount : 0;
        if (target != lastTarget || accN != lastAccN)
        {
            lastAccN = accN;
            foreach (var r in alwaysHidden) if (r != null) r.enabled = true;
            foreach (var r in bodyParts) if (r != null) r.enabled = true;
            Classify(target);
            foreach (var r in alwaysHidden) r.enabled = false;
            foreach (var r in bodyParts) r.enabled = false;
            bodyShown = false;
            lastTarget = target;
            eye = -1f;
            var c = GetComponent<Camera>();
            if (c != null) c.nearClipPlane = 0.05f;
        }

        // cuerpo visible solo al mirar hacia abajo (con histeresis)
        bool wantBody = bodyShown ? pitch > 16f : pitch > 26f;
        if (wantBody != bodyShown)
        {
            bodyShown = wantBody;
            foreach (var r in bodyParts) if (r != null) r.enabled = wantBody;
        }

        HandleInput();

        float wanted = eyeFactor * target.lossyScale.y;
        eye = eye < 0f ? wanted : Mathf.Lerp(eye, wanted, 12f * Time.deltaTime);

        // al mirar abajo la camara se adelanta un poco para ver el torso y los pies por fuera
        float t = Mathf.InverseLerp(15f, 70f, pitch);
        Vector3 fwd = new Vector3(Mathf.Sin(yaw * Mathf.Deg2Rad), 0f, Mathf.Cos(yaw * Mathf.Deg2Rad));
        transform.position = target.position + Vector3.up * eye + fwd * (0.3f * target.lossyScale.y * t);
        transform.rotation = Quaternion.Euler(pitch, yaw, 0f);
    }

    static bool InAcc(Renderer r, out bool head)
    {
        head = false;
        for (var t = r.transform; t != null; t = t.parent)
        {
            if (t.name.StartsWith("AccH_")) { head = true; return true; }
            if (t.name.StartsWith("Acc_")) return true;
        }
        return false;
    }

    // cabeza / pelo / ojos / cascos y todo lo que no sea malla con huesos: siempre ocultos. Resto: cuerpo.
    void Classify(Transform root)
    {
        alwaysHidden.Clear(); bodyParts.Clear();
        var all = root.GetComponentsInChildren<Renderer>(true);
        Bounds sb = default; bool has = false;
        foreach (var r in all)
            if (r is SkinnedMeshRenderer) { if (!has) { sb = r.bounds; has = true; } else sb.Encapsulate(r.bounds); }
        foreach (var r in all)
        {
            bool head;
            if (InAcc(r, out head)) { (head ? alwaysHidden : bodyParts).Add(r); continue; }
            if (!(r is SkinnedMeshRenderer)) { alwaysHidden.Add(r); continue; }   // capsula u otras formas
            string n = r.name.ToLowerInvariant();
            bool headPart = n.Contains("head") || n.Contains("hair") || n.Contains("eye") || n.Contains("brow") || n.Contains("lash")
                         || n.Contains("beard") || n.Contains("helmet") || n.Contains("horn") || n.Contains("ear") || n.Contains("teeth") || n.Contains("tooth")
                         || (has && r.bounds.center.y > sb.min.y + 0.8f * sb.size.y);
            (headPart ? alwaysHidden : bodyParts).Add(r);
        }
    }

    void HandleInput()
    {
        var mouse = Mouse.current;
        if (mouse != null && mouse.rightButton.isPressed)
            Look(mouse.delta.ReadValue() * mouseSensitivity);

        var ts = Touchscreen.current;
        if (ts == null) return;

        foreach (var t in ts.touches)
        {
            if (!t.press.isPressed) continue;
            Vector2 p = t.position.ReadValue();
            if (p.x < Screen.width * 0.4f) continue;   // zona del joystick
            if (EventSystem.current != null &&
                EventSystem.current.IsPointerOverGameObject(t.touchId.ReadValue())) continue;
            Look(t.delta.ReadValue() * touchSensitivity);
            break;
        }
    }

    void Look(Vector2 d)
    {
        yaw += d.x;
        pitch = Mathf.Clamp(pitch - d.y, -85f, 85f);
    }
}
