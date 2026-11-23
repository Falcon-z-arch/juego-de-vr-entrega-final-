using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

// Panel de inventario: se abre con el boton "Inventario" o la tecla I (ya no esta siempre en pantalla).
// Siempre muestra el artefacto. Si hay un cofre abierto, permite guardar y sacar objetos.
public class InventoryUI : MonoBehaviour
{
    public static InventoryUI I;

    public GameObject panel;
    public RectTransform listRoot;
    public Button rowTemplate;
    public TMP_Text title;
    public ArtifactController artifact;
    public PartyManager party;

    Chest chest;
    readonly List<GameObject> rows = new List<GameObject>();
    float acc;

    void Awake()
    {
        I = this;
        if (panel != null) panel.SetActive(false);
        if (rowTemplate != null) rowTemplate.gameObject.SetActive(false);
    }

    void OnEnable() => Inventory.Changed += Refresh;
    void OnDisable() => Inventory.Changed -= Refresh;

    void Update()
    {
        if (Keyboard.current != null && Keyboard.current.iKey.wasPressedThisFrame) Toggle();

        // mientras el artefacto esta en el suelo, el contador se actualiza cada segundo
        if (panel != null && panel.activeSelf && artifact != null && !artifact.IsCarried && !artifact.IsGone)
        {
            acc += Time.deltaTime;
            if (acc >= 1f) { acc = 0f; Refresh(); }
        }
    }

    // Conectar al boton "Inventario"
    public void Toggle()
    {
        if (panel.activeSelf) Close(); else Open(null);
    }

    public void Open(Chest c)
    {
        chest = c;
        panel.SetActive(true);
        Refresh();
    }

    // Conectar al boton "Cerrar"
    public void Close()
    {
        panel.SetActive(false);
        if (chest != null)
        {
            var c = chest;
            chest = null;
            c.Close();
        }
    }

    public void Refresh()
    {
        if (panel == null || !panel.activeSelf) return;

        foreach (var r in rows) { r.SetActive(false); Destroy(r); }
        rows.Clear();

        if (title != null) title.text = chest != null ? "Cofre" : "Inventario";

        // 1) El artefacto, siempre presente
        if (artifact != null)
        {
            if (artifact.IsGone)
                Row("Artefacto: destruido", null);
            else if (artifact.IsCarried)
                Row("Artefacto (llevado)  -  tocar para soltar", () => { artifact.Drop(party.Current); Refresh(); });
            else
                Row("Artefacto en el suelo: " + ArtifactController.Format(artifact.TimeLeft), null);
        }

        // 2) Objetos del inventario
        var mine = new List<string>(Inventory.Items);
        foreach (var name in mine)
        {
            string n = name;
            if (chest != null) Row("Guardar en el cofre: " + n, () => chest.Store(n));
            else Row(n, null);
        }
        if (mine.Count == 0 && chest == null) Row("(sin otros objetos)", null);

        // 3) Contenido del cofre
        if (chest != null)
        {
            foreach (var name in new List<string>(chest.contents))
            {
                string n = name;
                Row("Sacar del cofre: " + n, () => chest.Take(n));
            }
            if (chest.contents.Count == 0) Row("(el cofre esta vacio)", null);
        }
    }

    void Row(string text, System.Action onClick)
    {
        var b = Instantiate(rowTemplate, listRoot);
        b.gameObject.SetActive(true);
        b.name = "Fila";
        var t = b.GetComponentInChildren<TMP_Text>();
        if (t != null) t.text = text;
        if (onClick != null) b.onClick.AddListener(() => onClick());
        else b.interactable = false;
        rows.Add(b.gameObject);
    }
}
