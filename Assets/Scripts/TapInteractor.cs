using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;

// Detecta el toque en pantalla y llama a Interact() del objeto tocado si el personaje esta cerca.
public class TapInteractor : MonoBehaviour
{
    public PartyManager party;
    public Camera cam;
    public float maxDistance = 4f;

    void Start()
    {
        if (cam == null) cam = Camera.main;
    }

    void Update()
    {
        var p = Pointer.current;   // mouse en el editor, pantalla tactil en el celular
        if (p == null || !p.press.wasPressedThisFrame) return;
        if (EventSystem.current != null && EventSystem.current.IsPointerOverGameObject()) return;

        if (PuzzleCube.HeldCube != null)
        {
            PuzzleCube.HeldCube.ThrowFlat();
            return;
        }

        Ray ray = cam.ScreenPointToRay(p.position.ReadValue());
        var hits = Physics.RaycastAll(ray, 150f, ~0, QueryTriggerInteraction.Ignore);
        System.Array.Sort(hits, (a, b) => a.distance.CompareTo(b.distance));

        foreach (var h in hits)
        {
            if (h.collider.GetComponentInParent<PlayerMovement>() != null) continue; // ignora personajes

            var it = h.collider.GetComponentInParent<Interactable>();
            if (it == null) continue;   // PNJ, decoracion y muros no bloquean el toque: atraviesa hasta el interactuable

            var who = party.Current;
            float limit = it.interactDistance > 0f ? it.interactDistance : maxDistance;
            float d = Vector3.Distance(who.transform.position, it.transform.position);
            if (d > limit)
            {
                HUD.Say("Acercate mas a: " + it.prompt);
                return;
            }
            it.Interact(who);
            return;
        }

        // respaldo: si el rayo no dio con nada interactuable, busca uno cerca de la linea del toque
        var sh = Physics.SphereCastAll(ray, 0.9f, 60f, ~0, QueryTriggerInteraction.Collide);
        System.Array.Sort(sh, (a, b) => a.distance.CompareTo(b.distance));
        foreach (var h in sh)
        {
            var it = h.collider.GetComponentInParent<Interactable>();
            if (it == null || h.collider.GetComponentInParent<PlayerMovement>() != null) continue;
            var who = party.Current;
            float limit = it.interactDistance > 0f ? it.interactDistance : maxDistance;
            if (Vector3.Distance(who.transform.position, it.transform.position) > limit) { HUD.Say("Acercate mas a: " + it.prompt); return; }
            it.Interact(who);
            return;
        }
    }
}
