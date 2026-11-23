using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

// Menu de vendedor: tienda y conversacion en una sola ventana, con canvas propio (no depende del HUD).
//  Izquierda: retrato, nombre, monedas y pestanas.  Derecha: mejoras con barras de nivel o preguntas.
public static class VendorMenu
{
    static GameObject canvasGO, win;
    static RectTransform content;
    static TMP_Text coinsTxt, talkBody;
    static string vendor, tab;
    static Dialogue.Npc npc;

    static readonly Color Panel = new Color(0.10f, 0.08f, 0.07f, 0.97f);
    static readonly Color Side = new Color(0.17f, 0.12f, 0.09f, 1f);
    static readonly Color Gold = new Color(1f, 0.8f, 0.35f);
    static readonly Color Row = new Color(0.2f, 0.16f, 0.13f, 1f);

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    static void ResetStatics() { canvasGO = null; win = null; }

    public static bool IsOpen => canvasGO != null;

    public static void Open(string v)
    {
        if (canvasGO != null) Object.Destroy(canvasGO);   // restos de una apertura fallida
        canvasGO = null;
        vendor = v;
        npc = Dialogue.Get(v);
        tab = "shop";
        Modal.Begin();
        try { Build(); }
        catch (System.Exception ex)
        {
            Debug.LogError("VendorMenu (" + v + "): " + ex);
            Close();
            HUD.Say("Error al abrir la tienda: " + ex.Message);
        }
    }

    public static void Close()
    {
        if (canvasGO != null) Object.Destroy(canvasGO);
        canvasGO = null; win = null;
        Modal.End();
    }

    // ---------- construccion ----------
    static void Build()
    {
        if (Object.FindFirstObjectByType<EventSystem>() == null)
        {
            var es = new GameObject("EventSystem", typeof(EventSystem));
            es.AddComponent<UnityEngine.InputSystem.UI.InputSystemUIInputModule>();
        }
        canvasGO = new GameObject("MenuVendedor", typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
        var canvas = canvasGO.GetComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 600;
        var sc = canvasGO.GetComponent<CanvasScaler>();
        sc.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        sc.referenceResolution = new Vector2(1920f, 1080f);
        sc.matchWidthOrHeight = 0.5f;

        // fondo oscuro
        var dim = Img("Fondo", canvasGO.transform, new Color(0f, 0f, 0f, 0.55f));
        Stretch(dim.rectTransform);

        win = Img("Ventana", canvasGO.transform, Panel).gameObject;
        var wr = (RectTransform)win.transform;
        wr.anchorMin = wr.anchorMax = wr.pivot = new Vector2(0.5f, 0.5f);
        wr.sizeDelta = new Vector2(1500f, 860f);

        // marco dorado
        var frame = Img("Marco", wr, Gold * 0.6f);
        Rect(frame.rectTransform, new Vector2(0, 1), new Vector2(1, 1), new Vector2(0, -6), new Vector2(0, 0));
        var frame2 = Img("MarcoAbajo", wr, Gold * 0.6f);
        Rect(frame2.rectTransform, new Vector2(0, 0), new Vector2(1, 0), new Vector2(0, 0), new Vector2(0, 6));

        // barra lateral
        var side = Img("Lateral", wr, Side);
        Rect(side.rectTransform, new Vector2(0, 0), new Vector2(0.3f, 1), new Vector2(0, 6), new Vector2(0, -6));

        var portrait = Img("Retrato", side.transform, VendorColor());
        Rect(portrait.rectTransform, new Vector2(0.5f, 1), new Vector2(0.5f, 1), new Vector2(-110, -250), new Vector2(110, -40));
        var letter = Txt("Inicial", portrait.transform, vendor.Substring(0, 1), 140, Color.white, TextAlignmentOptions.Center);
        Stretch(letter.rectTransform);

        var name = Txt("Nombre", side.transform, npc.title, 40, Gold, TextAlignmentOptions.Center);
        Rect(name.rectTransform, new Vector2(0, 1), new Vector2(1, 1), new Vector2(20, -380), new Vector2(-20, -260));

        coinsTxt = Txt("Monedas", side.transform, "", 44, new Color(1f, 0.9f, 0.4f), TextAlignmentOptions.Center);
        Rect(coinsTxt.rectTransform, new Vector2(0, 1), new Vector2(1, 1), new Vector2(20, -470), new Vector2(-20, -390));

        MakeTabButton(side.transform, "TIENDA", "shop", -500);
        MakeTabButton(side.transform, "CONVERSAR", "talk", -600);

        var hint = Txt("Ayuda", side.transform, "Las mejoras son permanentes. Las monedas del banco las ganas subiendo la montana y las conservas aunque caigas.", 22, new Color(0.8f, 0.75f, 0.65f), TextAlignmentOptions.Center);
        Rect(hint.rectTransform, new Vector2(0, 0), new Vector2(1, 0), new Vector2(24, 16), new Vector2(-24, 150));

        // cerrar
        var close = Btn("Cerrar", wr, "X", new Color(0.55f, 0.15f, 0.15f), 60, Close);
        Rect(RT(close), new Vector2(1, 1), new Vector2(1, 1), new Vector2(-110, -110), new Vector2(-20, -20));

        // contenido
        var cont = new GameObject("Contenido", typeof(RectTransform));
        cont.transform.SetParent(wr, false);
        content = (RectTransform)cont.transform;
        Rect(content, new Vector2(0.3f, 0), new Vector2(1, 1), new Vector2(40, 40), new Vector2(-40, -130));

        var title = Txt("Titulo", wr, "", 56, Gold, TextAlignmentOptions.Left);
        title.name = "TituloVentana";
        Rect(title.rectTransform, new Vector2(0.3f, 1), new Vector2(1, 1), new Vector2(40, -120), new Vector2(-130, -10));

        Refresh();
    }

    static Color VendorColor()
    {
        switch (vendor)
        {
            case "Herrero": return new Color(0.6f, 0.25f, 0.18f);
            case "Alquimista": return new Color(0.2f, 0.55f, 0.35f);
            default: return new Color(0.5f, 0.38f, 0.15f);
        }
    }

    static void MakeTabButton(Transform parent, string label, string key, float y)
    {
        var b = Btn("Tab_" + key, parent, label, tab == key ? new Color(0.65f, 0.4f, 0.12f) : new Color(0.28f, 0.2f, 0.14f), 42, () => { tab = key; Refresh(); });
        b.name = "Tab_" + key;
        Rect(RT(b), new Vector2(0.5f, 1), new Vector2(0.5f, 1), new Vector2(-200, y - 80), new Vector2(200, y));
    }

    // ---------- contenido segun pestana ----------
    static void Refresh()
    {
        if (content == null) return;
        foreach (Transform t in content) Object.Destroy(t.gameObject);
        coinsTxt.text = "Monedas: " + RunState.Bank;
        var title = win.transform.Find("TituloVentana");
        if (title != null) title.GetComponent<TMP_Text>().text = tab == "shop" ? "Mejoras de " + vendor : "Conversacion";

        // recolorear pestanas
        foreach (var k in new[] { "shop", "talk" })
        {
            var tb = win.transform.Find("Lateral/Tab_" + k);
            if (tb != null) tb.GetComponent<Image>().color = tab == k ? new Color(0.65f, 0.4f, 0.12f) : new Color(0.28f, 0.2f, 0.14f);
        }

        if (tab == "shop") BuildShop(); else BuildTalk();
    }

    static void BuildShop()
    {
        float y = 0f;
        foreach (var u in RunState.Upgrades)
        {
            if (u.vendor != vendor) continue;
            int lvl = RunState.Level(u.id);
            bool max = lvl >= u.maxLevel;
            int cost = max ? 0 : u.cost[lvl];
            bool can = !max && RunState.Bank >= cost;

            var row = Img("Fila", content, Row);
            Rect(row.rectTransform, new Vector2(0, 1), new Vector2(1, 1), new Vector2(0, y - 160), new Vector2(0, y));
            y -= 180f;

            var nm = Txt("Nombre", row.transform, u.name, 44, Color.white, TextAlignmentOptions.Left);
            Rect(nm.rectTransform, new Vector2(0, 1), new Vector2(0.65f, 1), new Vector2(24, -56), new Vector2(0, -8));
            var ds = Txt("Desc", row.transform, u.desc, 30, new Color(0.85f, 0.82f, 0.75f), TextAlignmentOptions.Left);
            Rect(ds.rectTransform, new Vector2(0, 0.5f), new Vector2(0.65f, 0.5f), new Vector2(24, -40), new Vector2(0, 8));

            // barras de nivel
            for (int i = 0; i < u.maxLevel; i++)
            {
                var pip = Img("Nivel", row.transform, i < lvl ? Gold : new Color(0.3f, 0.26f, 0.22f));
                Rect(pip.rectTransform, new Vector2(0, 0), new Vector2(0, 0), new Vector2(24 + i * 64, 14), new Vector2(24 + i * 64 + 52, 34));
            }

            var up = u;
            string label = max ? "AL MAXIMO" : (can ? "COMPRAR\n" + cost + " monedas" : "FALTAN\n" + (cost - RunState.Bank) + " monedas");
            Color bc = max ? new Color(0.45f, 0.35f, 0.1f) : can ? new Color(0.2f, 0.55f, 0.25f) : new Color(0.25f, 0.22f, 0.22f);
            var buy = Btn("Comprar", row.transform, label, bc, 34, () => Buy(up));
            Rect(RT(buy), new Vector2(1, 0), new Vector2(1, 1), new Vector2(-330, 14), new Vector2(-18, -14));
        }
        if (y == 0f)
        {
            var empty = Txt("Vacio", content, "Este vendedor no tiene mejoras.", 40, Color.white, TextAlignmentOptions.Center);
            Stretch(empty.rectTransform);
        }
    }

    static void Buy(RunState.Upgrade u)
    {
        int lvl = RunState.Level(u.id);
        if (lvl >= u.maxLevel) { HUD.Say("Ya esta al maximo"); return; }
        int cost = u.cost[lvl];
        if (RunState.Bank < cost) { HUD.Say("Te faltan monedas"); Sfx.Beep(200f, 0.15f); return; }
        RunState.Bank -= cost;
        RunState.SetLevel(u.id, lvl + 1);
        Sfx.Beep(900f, 0.12f);
        HUD.Say("Compraste: " + u.name);
        Refresh();
    }

    static void BuildTalk()
    {
        var box = Img("Caja", content, new Color(0.16f, 0.13f, 0.11f));
        Rect(box.rectTransform, new Vector2(0, 0.42f), new Vector2(1, 1), new Vector2(0, 0), new Vector2(0, 0));
        talkBody = Txt("Texto", box.transform, npc.greet, 38, new Color(0.96f, 0.93f, 0.85f), TextAlignmentOptions.TopLeft);
        Rect(talkBody.rectTransform, new Vector2(0, 0), new Vector2(1, 1), new Vector2(28, 20), new Vector2(-28, -20));

        string[] q = { "¿Quien eres?", "¿Que sabes de la montana?", "¿Algun rumor?", "Dame un consejo" };
        string[] a = { npc.who, npc.mountain, npc.rumor, npc.tip };
        for (int i = 0; i < 4; i++)
        {
            int k = i;
            var b = Btn("Pregunta", content, q[i], new Color(0.3f, 0.22f, 0.15f), 36, () => { talkBody.text = a[k]; Sfx.Beep(600f, 0.05f); });
            int col = i % 2, rw = i / 2;
            float x0 = col * 0.5f;
            Rect(RT(b), new Vector2(x0, 0.4f), new Vector2(x0 + 0.5f, 0.4f), new Vector2(col == 0 ? 0 : 8, -110 - rw * 130), new Vector2(col == 0 ? -8 : 0, -20 - rw * 130));
        }
    }

    // ---------- helpers UI ----------
    static Image Img(string name, Transform parent, Color c)
    {
        var go = new GameObject(name, typeof(RectTransform), typeof(Image));
        go.transform.SetParent(parent, false);
        var im = go.GetComponent<Image>();
        im.color = c;
        return im;
    }

    static TMP_Text Txt(string name, Transform parent, string text, float size, Color c, TextAlignmentOptions al)
    {
        var go = new GameObject(name, typeof(RectTransform));
        go.transform.SetParent(parent, false);
        var t = go.AddComponent<TextMeshProUGUI>();
        t.text = text; t.fontSize = size; t.color = c; t.alignment = al;
        t.textWrappingMode = TextWrappingModes.Normal;
        t.raycastTarget = false;
        return t;
    }

    static Button Btn(string name, Transform parent, string label, Color c, float size, UnityEngine.Events.UnityAction onClick)
    {
        var im = Img(name, parent, c);
        var b = im.gameObject.AddComponent<Button>();
        b.targetGraphic = im;
        var cb = b.colors; cb.highlightedColor = new Color(1.2f, 1.2f, 1.2f); cb.pressedColor = new Color(0.8f, 0.8f, 0.8f); b.colors = cb;
        b.onClick.AddListener(onClick);
        var t = Txt("Texto", im.transform, label, size, Color.white, TextAlignmentOptions.Center);
        Stretch(t.rectTransform);
        return b;
    }


    static RectTransform RT(Component c) => (RectTransform)c.transform;

    static void Stretch(RectTransform r) { r.anchorMin = Vector2.zero; r.anchorMax = Vector2.one; r.offsetMin = r.offsetMax = Vector2.zero; }

    static void Rect(RectTransform r, Vector2 aMin, Vector2 aMax, Vector2 oMin, Vector2 oMax)
    {
        r.anchorMin = aMin; r.anchorMax = aMax; r.offsetMin = oMin; r.offsetMax = oMax;
    }
}
