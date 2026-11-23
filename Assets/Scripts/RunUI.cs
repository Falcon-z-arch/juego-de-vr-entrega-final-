using System;
using TMPro;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

// Ventanas del roguelike creadas por codigo (tiendas y altares) + marcador de monedas.
public static class UiKit
{
    public static Canvas MainCanvas()
    {
        foreach (var c in UnityEngine.Object.FindObjectsByType<Canvas>(FindObjectsSortMode.None))
            if (c.name == "Canvas") return c;
        return UnityEngine.Object.FindFirstObjectByType<Canvas>();
    }

    public static RectTransform Panel(string title, Vector2 size, out RectTransform content)
    {
        var canvas = MainCanvas();
        var go = new GameObject("Ventana_" + title, typeof(RectTransform), typeof(Image));
        go.transform.SetParent(canvas.transform, false);
        var rt = (RectTransform)go.transform;
        rt.anchorMin = rt.anchorMax = rt.pivot = new Vector2(0.5f, 0.5f);
        rt.sizeDelta = size;
        go.GetComponent<Image>().color = new Color(0.07f, 0.06f, 0.1f, 0.96f);

        var tgo = new GameObject("Titulo", typeof(RectTransform));
        tgo.transform.SetParent(rt, false);
        var trt = (RectTransform)tgo.transform;
        trt.anchorMin = new Vector2(0f, 1f); trt.anchorMax = new Vector2(1f, 1f); trt.pivot = new Vector2(0.5f, 1f);
        trt.anchoredPosition = new Vector2(0f, -20f); trt.sizeDelta = new Vector2(-40f, 90f);
        var t = tgo.AddComponent<TextMeshProUGUI>();
        t.text = title; t.fontSize = 50; t.alignment = TextAlignmentOptions.Center; t.color = new Color(1f, 0.8f, 0.4f);
        t.raycastTarget = false;
        go.name = "Ventana";

        var cgo = new GameObject("Contenido", typeof(RectTransform), typeof(VerticalLayoutGroup));
        cgo.transform.SetParent(rt, false);
        content = (RectTransform)cgo.transform;
        content.anchorMin = Vector2.zero; content.anchorMax = Vector2.one;
        content.offsetMin = new Vector2(40f, 140f); content.offsetMax = new Vector2(-40f, -120f);
        var v = cgo.GetComponent<VerticalLayoutGroup>();
        v.spacing = 14; v.childAlignment = TextAnchor.UpperCenter;
        v.childControlWidth = true; v.childControlHeight = true;
        v.childForceExpandWidth = true; v.childForceExpandHeight = false;
        return rt;
    }

    public static Button Btn(Transform parent, string label, float height, UnityAction onClick, int font = 34)
    {
        var go = new GameObject("Boton", typeof(RectTransform), typeof(Image), typeof(Button), typeof(LayoutElement));
        go.transform.SetParent(parent, false);
        go.GetComponent<Image>().color = new Color(0.22f, 0.2f, 0.34f, 0.95f);
        var le = go.GetComponent<LayoutElement>();
        le.preferredHeight = height; le.minHeight = height;

        var tgo = new GameObject("Texto", typeof(RectTransform));
        tgo.transform.SetParent(go.transform, false);
        var trt = (RectTransform)tgo.transform;
        trt.anchorMin = Vector2.zero; trt.anchorMax = Vector2.one;
        trt.offsetMin = new Vector2(16f, 4f); trt.offsetMax = new Vector2(-16f, -4f);
        var t = tgo.AddComponent<TextMeshProUGUI>();
        t.text = label; t.fontSize = font; t.alignment = TextAlignmentOptions.Center; t.color = Color.white;
        t.textWrappingMode = TextWrappingModes.Normal;
        t.raycastTarget = false;

        var b = go.GetComponent<Button>();
        b.onClick.AddListener(onClick);
        return b;
    }

    public static void CloseButton(RectTransform panel, UnityAction onClick)
    {
        var b = Btn(panel, "Cerrar", 90f, onClick, 40);
        var le = b.GetComponent<LayoutElement>();
        le.ignoreLayout = true;
        var rt = (RectTransform)b.transform;
        rt.anchorMin = rt.anchorMax = new Vector2(0.5f, 0f); rt.pivot = new Vector2(0.5f, 0f);
        rt.anchoredPosition = new Vector2(0f, 25f); rt.sizeDelta = new Vector2(320f, 90f);
    }
}

// Pausa mientras hay una ventana abierta
public static class Modal
{
    public static bool Open;
    static float prev = 1f;

    public static void Begin() { if (!Open) { prev = Time.timeScale > 0f ? Time.timeScale : 1f; Time.timeScale = 0f; Open = true; } }
    public static void End() { if (Open) { Time.timeScale = prev; Open = false; } }

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    static void ResetStatics() { Open = false; prev = 1f; }
}

// Vendedor de la Comarca: se toca y abre su tienda.
public class Vendor : Interactable
{
    public string vendorName = "Herrero";
    public static readonly System.Collections.Generic.List<Vendor> All = new System.Collections.Generic.List<Vendor>();

    void Awake() { prompt = vendorName; NearVendor.Ensure(); }
    void OnEnable() => All.Add(this);
    void OnDisable() => All.Remove(this);

    // Abre la charla sola al acercarte al mercado (no depende de NearVendor, triggers ni del modelo del vendedor).
    public float autoRadius = 5.5f;
    bool armed = true;
    PartyManager pm;

    void Update()
    {
        if (pm == null) { pm = FindFirstObjectByType<PartyManager>(); if (pm == null) return; }
        var cur = pm.Current;
        if (cur == null) return;
        Vector3 a = cur.transform.position, c = transform.position;
        float d = new Vector2(a.x - c.x, a.z - c.z).magnitude;
        if (d > autoRadius + 3f) armed = true;
        if (armed && d <= autoRadius && !Modal.Open)
        {
            armed = false;
            Debug.Log("Vendor " + vendorName + ": abre solo (dist " + d.ToString("0.0") + ")");
            try { Interact(cur); }
            catch (System.Exception ex) { Debug.LogError("Vendor " + vendorName + ": " + ex); HUD.Say("Error en la tienda: " + ex.Message); }
        }
    }

    public override void Interact(PlayerMovement who)
    {
        var actor = GetComponentInChildren<ActorAnim>();
        if (actor != null) actor.TalkFor(10f);
        VendorMenu.Open(vendorName);
    }
}

public static class ShopUI
{
    static GameObject window;

    public static void Open(string vendor)
    {
        Close();
        Modal.Begin();
        Build(vendor);
    }

    static void Build(string vendor)
    {
        if (window != null) UnityEngine.Object.Destroy(window);
        var panel = UiKit.Panel(vendor + "  -  Monedas: " + RunState.Bank, new Vector2(1300, 860), out var content);
        window = panel.gameObject;

        foreach (var u in RunState.Upgrades)
        {
            if (u.vendor != vendor) continue;
            int lvl = RunState.Level(u.id);
            bool max = lvl >= u.maxLevel;
            int cost = max ? 0 : u.cost[lvl];
            string text = max
                ? u.name + "  (MAX)\n" + u.desc
                : u.name + "  (Nv " + lvl + "/" + u.maxLevel + ")  -  " + cost + " monedas\n" + u.desc;
            var up = u;
            var b = UiKit.Btn(content, text, 130f, () => Buy(up, vendor), 32);
            if (max || RunState.Bank < cost) b.GetComponent<UnityEngine.UI.Image>().color = new Color(0.15f, 0.15f, 0.18f, 0.95f);
        }
        UiKit.CloseButton(panel, Close);
    }

    static void Buy(RunState.Upgrade u, string vendor)
    {
        int lvl = RunState.Level(u.id);
        if (lvl >= u.maxLevel) { HUD.Say("Ya esta al maximo"); return; }
        int cost = u.cost[lvl];
        if (RunState.Bank < cost) { HUD.Say("Te faltan monedas"); Sfx.Beep(200f, 0.15f); return; }

        RunState.Bank -= cost;
        RunState.SetLevel(u.id, lvl + 1);
        Sfx.Beep(900f, 0.12f);
        HUD.Say("Compraste: " + u.name);
        Build(vendor);
    }

    public static void Close()
    {
        if (window != null) UnityEngine.Object.Destroy(window);
        window = null;
        Modal.End();
    }
}

// Altar de reliquias: eliges 1 de 3 mejoras para esta partida.
public class BoonAltar : Interactable
{
    bool used;
    GameObject window;

    void Awake() => prompt = "Altar de reliquias";

    public override void Interact(PlayerMovement who)
    {
        if (used || window != null) return;
        Tutorial.Show("altar", "Altar de reliquias", "Elige UNA de tres reliquias.\nSolo duran esta subida, pero pueden cambiar tu forma de jugar.\nCada altar se usa una sola vez.", Open);
    }

    void Open()
    {
        if (used || window != null) return;
        Modal.Begin();
        var panel = UiKit.Panel("Elige una reliquia", new Vector2(1300, 760), out var content);
        window = panel.gameObject;

        foreach (var boon in RunState.RandomBoons(3))
        {
            var b0 = boon;
            UiKit.Btn(content, b0.name + "\n" + b0.desc + (RunState.Boon_(b0.id) > 0 ? "  (ya tienes " + RunState.Boon_(b0.id) + ")" : ""),
                      150f, () => Pick(b0), 36);
        }
    }

    void Pick(RunState.Boon b)
    {
        RunState.AddBoon(b.id);
        if (b.id == "heart" && PlayerHealth.I != null) PlayerHealth.I.AddLife();
        Sfx.Beep(1000f, 0.2f);
        HUD.Say("Reliquia: " + b.name);
        used = true;
        Destroy(window);
        window = null;
        Modal.End();
        var r = GetComponentInChildren<Renderer>();
        if (r != null) r.material.color = new Color(0.25f, 0.25f, 0.28f);
    }
}

// Marcador de monedas y piso
public class RunHud : MonoBehaviour
{
    TMP_Text label;

    void Start()
    {
        var canvas = UiKit.MainCanvas();
        if (canvas == null) { enabled = false; return; }

        var go = new GameObject("RunHudText", typeof(RectTransform));
        go.transform.SetParent(canvas.transform, false);
        var rt = (RectTransform)go.transform;
        rt.anchorMin = rt.anchorMax = new Vector2(1f, 1f);
        rt.pivot = new Vector2(1f, 1f);
        rt.anchoredPosition = new Vector2(-40f, -150f);
        rt.sizeDelta = new Vector2(1000f, 130f);
        var t = go.AddComponent<TextMeshProUGUI>();
        t.fontSize = 36;
        t.alignment = TextAlignmentOptions.TopRight;
        t.color = new Color(1f, 0.85f, 0.3f);
        t.raycastTarget = false;
        label = t;
    }

    void Update()
    {
        if (label == null) return;
        label.text = RunState.InRun
            ? (RunState.Floor >= 5 ? "CIMA" : Floor1()) + "\nMonedas de la partida: " + RunState.RunCoins + "   Banco: " + RunState.Bank
            : "Monedas: " + RunState.Bank;
    }

    static string Floor1() => RunState.Floor <= 0 ? "SUBIDA" : "PISO " + RunState.Floor + " / 4";
}
