using UnityEngine;
using UnityEngine.SceneManagement;

// Modo desarrollador: saltar a cualquier piso o al jefe para probarlo.
//  - En el Menu aparece el boton "DEV" (abajo a la izquierda).
//  - En la Comarca, si el modo esta activo, aparece "DEV" arriba al centro.
public static class DevMode
{
    public static int PendingJump = -1;     // piso al que saltar al cargar la Comarca (0 aldea, 1-4, 5 jefe)

    public static bool Enabled
    {
        get => PlayerPrefs.GetInt("devmode", 0) == 1;
        set { PlayerPrefs.SetInt("devmode", value ? 1 : 0); PlayerPrefs.Save(); }
    }

    public static bool God
    {
        get => Enabled && PlayerPrefs.GetInt("devgod", 0) == 1;
        set { PlayerPrefs.SetInt("devgod", value ? 1 : 0); PlayerPrefs.Save(); }
    }

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    static void ResetStatics() { PendingJump = -1; window = null; }

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    static void Hook()
    {
        SceneManager.sceneLoaded += (s, m) => OnScene(s.name);
        OnScene(SceneManager.GetActiveScene().name);
    }

    static GameObject window;

    static void OnScene(string scene)
    {
        window = null;
        if (scene != "Menu" && scene != "Comarca") return;
        if (scene == "Comarca" && !Enabled) return;
        var canvas = UiKit.MainCanvas();
        if (canvas == null) return;
        if (canvas.transform.Find("BotonDev") != null) return;

        var b = UiKit.Btn(canvas.transform, scene == "Menu" ? "DEV" : "DEV", 70f, () => OpenPanel(scene), 30);
        b.gameObject.name = "BotonDev";
        b.GetComponent<UnityEngine.UI.LayoutElement>().ignoreLayout = true;
        var rt = (RectTransform)b.transform;
        if (scene == "Menu")
        {
            rt.anchorMin = rt.anchorMax = rt.pivot = new Vector2(0f, 0f);
            rt.anchoredPosition = new Vector2(30f, 30f);
        }
        else
        {
            rt.anchorMin = rt.anchorMax = rt.pivot = new Vector2(0.5f, 1f);
            rt.anchoredPosition = new Vector2(0f, -30f);
        }
        rt.sizeDelta = new Vector2(180f, 70f);
    }

    static void OpenPanel(string scene)
    {
        if (window != null) { Close(); return; }
        Modal.Begin();
        var panel = UiKit.Panel("MODO DESARROLLADOR", new Vector2(1000, 1060), out var content);
        window = panel.gameObject;
        UiKit.Btn(content, "Inmortal: " + (God ? "SI" : "NO") + "  (toca para cambiar)", 80f, () => { bool g = !God; Enabled = true; God = g; Close(); OpenPanel(scene); }, 34);
        string[] names = { "Aldea (inicio)", "Piso 1: Bosque Susurrante", "Piso 2: Laberinto de Raices", "Piso 3: Paso Helado", "Piso 4: Ruinas Antiguas", "JEFE FINAL: Cronarca" };
        for (int i = 0; i < names.Length; i++)
        {
            int n = i;
            UiKit.Btn(content, names[i], 70f, () => Go(scene, n), 34);
        }
        if (scene == "Comarca")
        {
            for (int i = 1; i <= 4; i++)
            {
                int f = i;
                UiKit.Btn(content, "Abrir la puerta del piso " + f, 60f, () => { Close(); if (RunManager.I != null) RunManager.I.DevOpenDoor(f); }, 30);
            }
            UiKit.Btn(content, "Abrir las puertas de los 4 pisos", 60f, () => { Close(); if (RunManager.I != null) RunManager.I.DevOpenDoors(); }, 30);
        }
        UiKit.CloseButton(panel, Close);
    }

    static void Close()
    {
        if (window != null) Object.Destroy(window);
        window = null;
        Modal.End();
    }

    static void Go(string scene, int n)
    {
        Close();
        Enabled = true;
        if (scene == "Comarca" && RunManager.I != null) { RunManager.I.DevJump(n); return; }
        PendingJump = n;
        Time.timeScale = 1f;
        if (Application.CanStreamedLevelBeLoaded("Comarca")) SceneManager.LoadScene("Comarca");
    }
}
