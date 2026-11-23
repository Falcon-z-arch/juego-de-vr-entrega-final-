using TMPro;
using UnityEditor;
using UnityEditor.Events;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem.UI;
using UnityEngine.UI;
using static ZonaPruebasBuilder;

// Menu principal + "Construir TODO" (menu, zona de pruebas, volcan y galeria en un solo paso).
public static class MenuBuilder
{
    [MenuItem("Certamen2/0 - Construir TODO (menu + escenas)")]
    static void BuildAll()
    {
        if (TMP_Settings.instance == null)
        {
            EditorUtility.DisplayDialog("Falta TextMeshPro",
                "Primero importa los recursos: Window > TextMeshPro > Import TMP Essential Resources. Luego vuelve a ejecutar este menu.", "OK");
            return;
        }
        if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;

        MedievalKit.Prepare();
        NatureKit.Prepare();
        FantasyKit.Prepare();
        DungeonKit.Prepare();
        UalKit.Prepare();
        RacesKit.Prepare();
        ZombieKit.Prepare();
        ClothingKit.Prepare();  // props de fantasia (tiendas)   // naturaleza y props RPG (si estan en el proyecto)   // materiales del kit medieval (si esta en el proyecto)

        quiet = true;
        try
        {
            Build(false);          // zona de pruebas
            Build(false, true);    // galeria
            Build(false, false, true);   // comarca (aldea)
            BuildMenu();           // menu (queda abierto: dale Play desde aqui)
        }
        finally { quiet = false; }

        EditorUtility.DisplayDialog("Listo",
            "Se crearon Menu, Comarca (aldea + montana completa), ZonaPruebas y Galeria y quedaron en Build Profiles.\n\nDale Play con la escena Menu abierta.", "OK");
    }

    [MenuItem("Certamen2/4 - Construir menu principal")]
    static void BuildMenuOnly()
    {
        if (TMP_Settings.instance == null)
        {
            EditorUtility.DisplayDialog("Falta TextMeshPro", "Importa TMP Essential Resources primero.", "OK");
            return;
        }
        if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;
        BuildMenu();
        EditorUtility.DisplayDialog("Listo", "Menu creado: " + MenuPath, "OK");
    }

    static void BuildMenu()
    {
        EnsureFolder("Assets", "Scenes");
        EnsureFolder("Assets", "Materials");

        var scene = EditorSceneManager.NewScene(NewSceneSetup.DefaultGameObjects, NewSceneMode.Single);

        // fondo oscuro con brillo de lava
        var cam = Camera.main;
        cam.clearFlags = CameraClearFlags.SolidColor;
        cam.backgroundColor = new Color(0.07f, 0.025f, 0.02f);
        cam.transform.position = Vector3.zero;
        cam.transform.rotation = Quaternion.identity;
        RenderSettings.ambientMode = UnityEngine.Rendering.AmbientMode.Flat;
        RenderSettings.ambientLight = new Color(0.25f, 0.15f, 0.12f);

        var ascua = Prim(PrimitiveType.Sphere, "Ascua", new Vector3(4.2f, -0.2f, 9f), Vector3.one * 2.4f, Mat("Artefacto", new Color(1f, 0.85f, 0.2f), true), null, false);
        ascua.AddComponent<ModelSlot>().slotName = "Artefacto";
        var lg = new GameObject("LuzAscua");
        lg.transform.position = new Vector3(3f, 0.5f, 6f);
        var l = lg.AddComponent<Light>();
        l.type = LightType.Point; l.range = 20f; l.intensity = 25f; l.color = new Color(1f, 0.5f, 0.15f);

        // ---------- UI ----------
        var canvasGO = new GameObject("Canvas", typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
        canvasGO.GetComponent<Canvas>().renderMode = RenderMode.ScreenSpaceOverlay;
        var scaler = canvasGO.GetComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920, 1080);
        scaler.matchWidthOrHeight = 0.5f;

        var es = new GameObject("EventSystem", typeof(EventSystem));
        var module = es.AddComponent<InputSystemUIInputModule>();
        module.AssignDefaultActions();

        var top = new Vector2(0.5f, 1f);
        var title = UIText("Titulo", canvasGO.transform, "ASCUA", 190, TextAlignmentOptions.Center, top, top, new Vector2(-250, -50), new Vector2(1200, 250));
        title.color = new Color(1f, 0.75f, 0.3f);
        title.fontStyle = FontStyles.Bold;
        var sub = UIText("Subtitulo", canvasGO.transform, "El peso de Karnak", 60, TextAlignmentOptions.Center, top, top, new Vector2(-250, -300), new Vector2(1200, 90));
        sub.color = new Color(1f, 0.9f, 0.8f);

        var mc = new GameObject("MenuController").AddComponent<MenuController>();
        mc.gameObject.AddComponent<SceneMusic>().key = "menu";

        var mid = new Vector2(0.5f, 0.5f);
        var bStory = MenuButton("Modo Historia", canvasGO.transform, mid, new Vector2(-250, 60), 56);
        var bTest = MenuButton("Probar controles", canvasGO.transform, mid, new Vector2(-250, -70), 56);
        var bGal = MenuButton("Galeria de objetos", canvasGO.transform, mid, new Vector2(-250, -200), 56);
        var bVr = MenuButton("Modo VR: NO", canvasGO.transform, mid, new Vector2(-250, -330), 56);
        mc.vrLabel = bVr.GetComponentInChildren<TextMeshProUGUI>();
        var bQuit = MenuButton("Salir", canvasGO.transform, mid, new Vector2(-250, -450), 44);
        bQuit.GetComponent<RectTransform>().sizeDelta = new Vector2(400, 90);

        UnityEventTools.AddPersistentListener(bStory.onClick, new UnityAction(mc.StartStory));
        UnityEventTools.AddPersistentListener(bTest.onClick, new UnityAction(mc.StartTest));
        UnityEventTools.AddPersistentListener(bGal.onClick, new UnityAction(mc.StartGallery));
        UnityEventTools.AddPersistentListener(bVr.onClick, new UnityAction(mc.ToggleVR));
        UnityEventTools.AddPersistentListener(bQuit.onClick, new UnityAction(mc.Quit));

        UIText("Info", canvasGO.transform, "Historia: Comarca, 4 pisos de mazmorra y el Cronarca  |  Controles: zona de pruebas  |  Galeria: todos los objetos",
            28, TextAlignmentOptions.Center, new Vector2(0.5f, 0), new Vector2(0.5f, 0), new Vector2(-250, 20), new Vector2(1500, 50)).color = new Color(1f, 1f, 1f, 0.55f);

        EditorSceneManager.MarkSceneDirty(scene);
        EditorSceneManager.SaveScene(scene, MenuPath);
        UpdateBuildSettings();
        AssetDatabase.SaveAssets();
        Debug.Log("Menu creado en " + MenuPath);
    }

    static Button MenuButton(string label, Transform parent, Vector2 anchor, Vector2 pos, int font)
    {
        var b = UIButton(label, parent, anchor, anchor, pos, new Vector2(760, 120), font);
        b.GetComponent<Image>().color = new Color(0.45f, 0.15f, 0.05f, 0.92f);
        return b;
    }
}
