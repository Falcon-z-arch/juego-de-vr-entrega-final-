using System.Collections;
using UnityEngine;

// Puerta de Karnak (final de la aldea): al abrirla empieza la subida y el bombardeo.
public class RunGate : Interactable
{
    public Transform portal;     // (antiguo) hoja que sube al abrirse
    public Transform[] leaves;   // hojas con pivote en las columnas: se abren hacia afuera
    bool opened;

    void Awake()
    {
        prompt = "Abrir la Puerta de Karnak";
        if (interactDistance <= 0f) interactDistance = 9f;
    }

    public override void Interact(PlayerMovement who)
    {
        if (opened) return;
        if (RunManager.I == null) { HUD.Say("Falta RunManager. Ejecuta Certamen2 > 0 - Construir TODO"); return; }
        opened = true;
        Sfx.Beep(300f, 0.5f);
        RunManager.I.StartClimb();
        StartCoroutine(Open());
    }

    IEnumerator Open()
    {
        var col = GetComponent<Collider>();
        if (col != null) col.enabled = false;
        if (leaves != null && leaves.Length > 0)
        {
            float tt = 0f;
            while (tt < 1f)
            {
                tt += Time.deltaTime / 2.2f;
                float e = Mathf.SmoothStep(0f, 1f, tt);
                foreach (var l in leaves)
                    if (l != null) l.localRotation = Quaternion.Euler(0f, -100f * Mathf.Sign(l.localPosition.z) * e, 0f);
                yield return null;
            }
            yield break;
        }
        if (portal == null) yield break;
        Vector3 a = portal.position, b = a + Vector3.up * 7f;
        float t = 0f;
        while (t < 1f)
        {
            t += Time.deltaTime / 1.6f;
            portal.position = Vector3.Lerp(a, b, t);
            yield return null;
        }
        portal.gameObject.SetActive(false);
    }
}
