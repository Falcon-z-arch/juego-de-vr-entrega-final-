using TMPro;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;

// Boton "Hablar con X" al acercarte a un vendedor (mesa/tienda). Canvas propio: no depende del HUD.
public class NearVendor : MonoBehaviour
{
    public float range = 7f;       // metros hasta el borde de la tienda (no hasta su centro)
    PartyManager party;
    GameObject root;
    TMP_Text label;
    Vendor current;
    float logT;
    bool armed = true;   // la tienda se abre sola al acercarse; se rearma al alejarte
    public float openRange = 2.2f;
    bool buildFailed, near;

    static NearVendor inst;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    static void ResetStatics() { inst = null; }

    // lo llama cada Vendor al despertar (si la instancia anterior murio con su escena, crea otra)
    public static void Ensure()
    {
        if (inst != null) return;
        inst = new GameObject("NearVendor").AddComponent<NearVendor>();
        Debug.Log("NearVendor: activo (tiendas v4)");
    }

    void Start() { Invoke(nameof(Guarantee), 1.0f); Invoke(nameof(Guarantee), 4.0f); }

    // Red de seguridad: si la Comarca debe tener 3 vendedores y falta alguno, se repara al ejecutar (copia el mismo Vendor de los otros).
    void Guarantee()
    {
        if (Vendor.All.Count == 0 || GameObject.Find("Tienda_Herrero") == null || GameObject.Find("Tienda_Alquimista") == null) return;
        string[] names = { "Herrero", "Alquimista", "Mercader" };
        Vector3[] poss = { new Vector3(-1.95f, 0f, -14.5f), new Vector3(3.95f, 0f, 14.5f), new Vector3(36.05f, 0f, -13.5f) };
        for (int k = 0; k < names.Length; k++)
        {
            string name = names[k]; Vector3 pos = poss[k];
            bool has = false;
            foreach (var x in Vendor.All) if (x != null && x.vendorName == name) has = true;
            if (has) continue;
            var go = GameObject.Find("Tienda_" + name);
            if (go == null) { go = new GameObject("Tienda_" + name); go.transform.position = pos; }
            else if (!go.activeSelf) go.SetActive(true);
            var v = go.GetComponent<Vendor>();
            if (v == null) v = go.AddComponent<Vendor>();
            v.enabled = true;
            v.vendorName = name; v.prompt = name; v.interactDistance = 11f;
            if (go.GetComponent<SphereCollider>() == null)
            {
                var sc = go.AddComponent<SphereCollider>();
                sc.isTrigger = true; sc.center = new Vector3(0f, 1.2f, 0f); sc.radius = 2.8f;
            }
            Debug.LogWarning("NearVendor: vendedor " + name + " faltaba y se reparo al ejecutar");
        }
    }

    void Update()
    {
        if (Vendor.All.Count == 0) { dbg = "sin vendedores"; if (root != null) root.SetActive(false); return; }
        if (party == null) { party = FindFirstObjectByType<PartyManager>(); if (party == null) { dbg = "sin PartyManager"; return; } }
        var p = party.Current;
        if (p == null) { dbg = "sin personaje"; return; }
        if (root == null && !buildFailed) { try { Build(); } catch (System.Exception ex) { buildFailed = true; Debug.LogError("NearVendor canvas: " + ex); } }

        Vendor best = null;
        float bd = range;
        Vector3 pos = p.transform.position;
        foreach (var v in Vendor.All)
        {
            if (v == null) continue;
            float d = DistTo(v, pos);
            if (d < bd) { bd = d; best = v; }
        }
        dbg = best != null ? "cerca de " + best.vendorName + " a " + bd.ToString("0.0") + " m" : "lejos de vendedores";
        bool show = best != null && !Modal.Open;
        near = show;
        if (best == null || bd > openRange + 3f) armed = true;
        if (show && armed && bd <= openRange && !Modal.Open)
        {
            armed = false;
            current = best;
            Talk();
        }
        if (show) { current = best; if (label != null) label.text = "Hablar con " + best.vendorName + "  [E]"; }
        if (root != null && root.activeSelf != show) root.SetActive(show);

        if (show && UnityEngine.InputSystem.Keyboard.current != null &&
            UnityEngine.InputSystem.Keyboard.current.eKey.wasPressedThisFrame) Talk();

        if (Time.unscaledTime > logT)
        {
            logT = Time.unscaledTime + 3f;
            if (best != null) Debug.Log("NearVendor: cerca de " + best.vendorName + " a " + bd.ToString("0.0") + " m, Modal.Open=" + Modal.Open);
        }
    }

    // distancia horizontal al borde de la tienda (huella ~6x5 m alrededor de su centro); no depende del modelo del vendedor
    static float DistTo(Vendor v, Vector3 pos)
    {
        Vector3 c = v.transform.position;
        float d = new Vector2(pos.x - c.x, pos.z - c.z).magnitude;
        return Mathf.Max(0f, d - 2.5f);
    }

    // Respaldo: boton y estado dibujados con IMGUI (no dependen de Canvas ni EventSystem)
    string dbg = "";
    void OnGUI()
    {
        bool showBtn = near && current != null && !Modal.Open;
        if (DevMode.Enabled)
        {
            var st = new GUIStyle(GUI.skin.label) { fontSize = Mathf.Max(14, Screen.height / 45) };
            GUI.color = Color.yellow;
            GUI.Label(new Rect(10, Screen.height - st.fontSize * 2.4f, Screen.width, st.fontSize * 2f),
                "Vendedores: " + Vendor.All.Count + " (" + string.Join(",", Vendor.All.ConvertAll(v => v != null ? v.vendorName : "?")) + ")  Modal: " + Modal.Open + "  tiendaAbierta: " + VendorMenu.IsOpen + "  timeScale: " + Time.timeScale.ToString("0.##") + "  " + dbg, st);
            GUI.color = Color.white;
        }
        if (!showBtn) return;
        float w = Screen.width * 0.34f, h = Screen.height * 0.11f;
        var bs = new GUIStyle(GUI.skin.button) { fontSize = Mathf.Max(18, Screen.height / 28), fontStyle = FontStyle.Bold };
        if (GUI.Button(new Rect((Screen.width - w) / 2f, Screen.height * 0.62f, w, h), "HABLAR con " + current.vendorName, bs)) Talk();
    }

    void Talk()
    {
        if (current == null || party == null || party.Current == null) return;
        try { current.Interact(party.Current); }
        catch (System.Exception ex) { Debug.LogError("Tienda " + current.vendorName + ": " + ex); HUD.Say("Error en la tienda: " + ex.Message); }
    }

    void Build()
    {
        if (FindFirstObjectByType<EventSystem>() == null)
        {
            var es = new GameObject("EventSystem", typeof(EventSystem));
            es.AddComponent<UnityEngine.InputSystem.UI.InputSystemUIInputModule>();
        }
        var cgo = new GameObject("CanvasHablar", typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
        cgo.transform.SetParent(transform, false);
        var canvas = cgo.GetComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 500;
        var sc = cgo.GetComponent<CanvasScaler>();
        sc.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        sc.referenceResolution = new Vector2(1920f, 1080f);
        sc.matchWidthOrHeight = 0.5f;

        root = new GameObject("BotonHablar", typeof(RectTransform), typeof(Image), typeof(Button));
        root.transform.SetParent(cgo.transform, false);
        var rt = (RectTransform)root.transform;
        rt.anchorMin = rt.anchorMax = rt.pivot = new Vector2(0.5f, 0f);
        rt.anchoredPosition = new Vector2(0f, 260f);
        rt.sizeDelta = new Vector2(640f, 120f);
        root.GetComponent<Image>().color = new Color(0.65f, 0.35f, 0.08f, 0.97f);
        root.GetComponent<Button>().onClick.AddListener(Talk);

        var tgo = new GameObject("Texto", typeof(RectTransform));
        tgo.transform.SetParent(root.transform, false);
        var trt = (RectTransform)tgo.transform;
        trt.anchorMin = Vector2.zero; trt.anchorMax = Vector2.one; trt.offsetMin = trt.offsetMax = Vector2.zero;
        var t = tgo.AddComponent<TextMeshProUGUI>();
        t.fontSize = 44; t.alignment = TextAlignmentOptions.Center; t.color = Color.white; t.raycastTarget = false;
        label = t;
        root.SetActive(false);
    }
}
