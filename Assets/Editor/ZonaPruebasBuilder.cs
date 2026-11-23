using TMPro;
using UnityEditor;
using UnityEditor.Build;
using UnityEditor.Events;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem.OnScreen;
using UnityEngine.InputSystem.UI;
using UnityEngine.UI;

// Construye TODA la escena con un clic: menu  Certamen2 > 2 - Construir zona de pruebas
// Debe estar en una carpeta llamada Editor (Assets/Editor/).
public static class ZonaPruebasBuilder
{
    const string ScenePath = "Assets/Scenes/ZonaPruebas.unity";
    const string VolcanPath = "Assets/Scenes/Volcan.unity";
    internal const string MenuPath = "Assets/Scenes/Menu.unity";
    internal const string GaleriaPath = "Assets/Scenes/Galeria.unity";
    internal const string ComarcaPath = "Assets/Scenes/Comarca.unity";
    internal static bool quiet;   // true = no mostrar dialogos (lo usa "Construir TODO")

    // ------------------------------------------------------------------ Android
    [MenuItem("Certamen2/1 - Configurar Android")]
    static void ConfigAndroid()
    {
        PlayerSettings.SetScriptingBackend(NamedBuildTarget.Android, ScriptingImplementation.IL2CPP);
        PlayerSettings.Android.targetArchitectures = AndroidArchitecture.ARM64;
        PlayerSettings.defaultInterfaceOrientation = UIOrientation.LandscapeLeft;
        Debug.Log("Android configurado: IL2CPP + ARM64 + Landscape Left.");
        EditorUtility.DisplayDialog("Android",
            "Configurado: IL2CPP, ARM64 y Landscape Left.\n\nRecuerda poner a mano Player Settings > Other Settings > Active Input Handling = Input System Package (New).", "OK");
    }

    // ------------------------------------------------------------------ Escena
    [MenuItem("Certamen2/2 - Construir zona de pruebas")]
    static void BuildZona() => Build(false);

    [MenuItem("Certamen2/3 - Construir escenario del volcan (juego principal)")]
    static void BuildVolcan() => Build(true);

    [MenuItem("Certamen2/5 - Construir galeria de objetos")]
    static void BuildGaleria() => Build(false, true);

    internal static void Build(bool volcano, bool gallery = false, bool comarca = false)
    {
        if (TMP_Settings.instance == null)
        {
            EditorUtility.DisplayDialog("Falta TextMeshPro",
                "Primero importa los recursos: Window > TextMeshPro > Import TMP Essential Resources. Luego vuelve a ejecutar este menu.", "OK");
            return;
        }
        if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;

        EnsureFolder("Assets", "Scenes");
        EnsureFolder("Assets", "Materials");

        var scene = EditorSceneManager.NewScene(NewSceneSetup.DefaultGameObjects, NewSceneMode.Single);

        Vector3 start;
        LavaCrater crater;
        if (comarca) crater = ComarcaBuilder.Build(out start);
        else if (gallery) crater = GaleriaBuilder.Build(out start);
        else if (volcano) crater = VolcanBuilder.Build(out start);
        else { crater = BuildCorridor(); start = new Vector3(-22f, 0f, 0f); }

        // ---------- Personajes ----------
        var hobbit = MakeChar("Hobbit", Role.Hobbit, start + new Vector3(0f, 0.85f, -1.5f), new Color(0.4f, 0.7f, 0.3f), 0.8f);
        var mage = MakeChar("Mago", Role.Mage, start + new Vector3(0f, 1.05f, 0f), new Color(0.5f, 0.3f, 0.8f), 1f);
        var engineer = MakeChar("Ingeniero", Role.Engineer, start + new Vector3(0f, 1.05f, 1.5f), new Color(0.9f, 0.55f, 0.15f), 1f);
        var fighter = MakeChar("Peleador", Role.Fighter, start + new Vector3(0f, 1.05f, 3f), new Color(0.75f, 0.15f, 0.15f), 1.1f);   // solo VR

        // ---------- Artefacto (siempre lo llevas; se ve en un costado) ----------
        var artGO = Prim(PrimitiveType.Sphere, "Artefacto", hobbit.transform.position, Vector3.one * 0.35f, Mat("Artefacto", new Color(1f, 0.85f, 0.2f), true));
        artGO.AddComponent<ModelSlot>().slotName = "Artefacto";
        var artifact = artGO.AddComponent<ArtifactController>();

        // ---------- Camara ----------
        var cam = Camera.main;
        var cf = cam.gameObject.AddComponent<CameraFollow>();
        cf.target = hobbit.transform;
        cam.transform.position = hobbit.transform.position + new Vector3(-9f, 6f, 0f);
        cam.transform.LookAt(hobbit.transform.position);

        // ---------- GameManager ----------
        var gm = new GameObject("GameManager");
        var party = gm.AddComponent<PartyManager>();
        var ability = gm.AddComponent<AbilityController>();
        var tap = gm.AddComponent<TapInteractor>();
        party.characters = new[] { hobbit, mage, engineer, fighter };
        party.cameraFollow = cf;
        party.artifact = artifact;
        ability.party = party;
        tap.party = party;
        tap.cam = cam;
        artifact.party = party;
        if (crater != null) crater.artifact = artifact;

        var rocks = gm.AddComponent<FallingRockSpawner>();
        rocks.party = party;
        rocks.minAltitude = volcano || comarca ? -100f : 2f;
        rocks.enabled = !gallery && !comarca;
        gm.AddComponent<PlayerHealth>();

        if (volcano || comarca)
        {
            var director = gm.AddComponent<AvalancheDirector>();
            director.spawner = rocks;

            // roguelike: genera los pisos, lleva a la cima y maneja derrota / victoria
            var run = gm.AddComponent<RunManager>();
            run.party = party;
            run.spawner = rocks;
            run.director = director;
            run.cam = cf;
            run.summit = Object.FindFirstObjectByType<SummitArena>();
            if (comarca) run.zones = ComarcaBuilder.Zones;
        }

        // musica y cinematica
        gm.AddComponent<SceneMusic>().key = comarca ? "comarca" : gallery ? "galeria" : volcano ? "volcan" : "pruebas";
        if (comarca) gm.AddComponent<IntroCinematic>();

        // Rig VR: no hace nada si no hay casco conectado
        var vrGO = new GameObject("VRRig");
        var vr = vrGO.AddComponent<VRRig>();
        vr.party = party;
        vr.ability = ability;

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

        var knobSprite = AssetDatabase.GetBuiltinExtraResource<Sprite>("UI/Skin/Knob.psd");

        // Joystick
        var jRt = UIRect("Joystick", canvasGO.transform);
        SetRect(jRt, new Vector2(0, 0), new Vector2(0, 0), new Vector2(120, 120), new Vector2(320, 320));
        var jImg = jRt.gameObject.AddComponent<Image>();
        jImg.sprite = knobSprite; jImg.color = new Color(1, 1, 1, 0.25f); jImg.raycastTarget = false;

        var kRt = UIRect("Knob", jRt);
        SetRect(kRt, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(140, 140));
        var kImg = kRt.gameObject.AddComponent<Image>();
        kImg.sprite = knobSprite; kImg.color = new Color(1, 1, 1, 0.8f);
        var stick = kRt.gameObject.AddComponent<OnScreenStick>();
        stick.movementRange = 110f;
        stick.controlPath = "<Gamepad>/leftStick";

        // Botones (abajo a la derecha)
        var br = new Vector2(1, 0);
        var btnJump = UIButton("Saltar", canvasGO.transform, br, br, new Vector2(-40, 40), new Vector2(300, 100), 42);
        var btnAbility = UIButton("Habilidad", canvasGO.transform, br, br, new Vector2(-40, 160), new Vector2(300, 100), 42);
        var btnSwitch = UIButton("Personajes", canvasGO.transform, br, br, new Vector2(-40, 280), new Vector2(300, 100), 42);
        var btnInv = UIButton("Inventario", canvasGO.transform, br, br, new Vector2(-40, 400), new Vector2(300, 100), 42);
        UnityEventTools.AddPersistentListener(btnJump.onClick, new UnityAction(party.Jump));
        UnityEventTools.AddPersistentListener(btnAbility.onClick, new UnityAction(ability.UseAbility));

        // Textos del HUD
        var msg = UIText("Mensaje", canvasGO.transform, "", 44, TextAlignmentOptions.Center,
            new Vector2(0.5f, 0), new Vector2(0.5f, 0), new Vector2(0, 40), new Vector2(1200, 120));
        var timer = UIText("TemporizadorArtefacto", canvasGO.transform, "", 42, TextAlignmentOptions.Center,
            new Vector2(0.5f, 1), new Vector2(0.5f, 1), new Vector2(0, -40), new Vector2(1200, 80));
        timer.color = new Color(1f, 0.85f, 0.3f);

        var hudGO = new GameObject("HUD");
        var hud = hudGO.AddComponent<HUD>();
        hud.messageText = msg;
        hud.artifactText = timer;

        var lifeTxt = UIText("Vidas", canvasGO.transform, "", 44, TextAlignmentOptions.TopLeft,
            new Vector2(0, 1), new Vector2(0, 1), new Vector2(40, -40), new Vector2(700, 80));
        lifeTxt.color = new Color(1f, 0.45f, 0.45f);
        hud.lifeText = lifeTxt;

        // mira (punto central de la vista)
        var dot = UIRect("Mira", canvasGO.transform);
        SetRect(dot, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(10, 10));
        var dotImg = dot.gameObject.AddComponent<Image>();
        dotImg.sprite = knobSprite; dotImg.color = new Color(1f, 1f, 1f, 0.85f); dotImg.raycastTarget = false;

        // boton para alternar entre el volcan y la zona de pruebas
        var btnScene = UIButton("Menu", canvasGO.transform, new Vector2(1, 1), new Vector2(1, 1), new Vector2(-40, -40), new Vector2(300, 90), 36);
        var switcher = canvasGO.AddComponent<SceneSwitcher>();
        UnityEventTools.AddPersistentListener(btnScene.onClick, new UnityAction(switcher.ToMenu));

        // Panel "Elegir personaje"
        var charPanel = UIPanel("PanelPersonajes", canvasGO.transform, new Vector2(1100, 460));
        UIText("Titulo", charPanel, "Elegir personaje", 54, TextAlignmentOptions.Center,
            new Vector2(0.5f, 1), new Vector2(0.5f, 1), new Vector2(0, -25), new Vector2(1000, 90));
        var mid = new Vector2(0.5f, 0.5f);
        var bH = UIButton("Hobbit", charPanel, mid, mid, new Vector2(-340, 10), new Vector2(300, 130), 44);
        var bM = UIButton("Mago", charPanel, mid, mid, new Vector2(0, 10), new Vector2(300, 130), 44);
        var bE = UIButton("Ingeniero", charPanel, mid, mid, new Vector2(340, 10), new Vector2(300, 130), 44);
        var bClose = UIButton("Cerrar", charPanel, new Vector2(0.5f, 0), new Vector2(0.5f, 0), new Vector2(0, 30), new Vector2(300, 90), 40);

        var panelToggle = canvasGO.AddComponent<PanelToggle>();
        panelToggle.panel = charPanel.gameObject;
        UnityEventTools.AddPersistentListener(btnSwitch.onClick, new UnityAction(panelToggle.Toggle));
        UnityEventTools.AddPersistentListener(bH.onClick, new UnityAction(party.SelectHobbit));
        UnityEventTools.AddPersistentListener(bH.onClick, new UnityAction(panelToggle.Hide));
        UnityEventTools.AddPersistentListener(bM.onClick, new UnityAction(party.SelectMage));
        UnityEventTools.AddPersistentListener(bM.onClick, new UnityAction(panelToggle.Hide));
        UnityEventTools.AddPersistentListener(bE.onClick, new UnityAction(party.SelectEngineer));
        UnityEventTools.AddPersistentListener(bE.onClick, new UnityAction(panelToggle.Hide));
        UnityEventTools.AddPersistentListener(bClose.onClick, new UnityAction(panelToggle.Hide));
        charPanel.gameObject.SetActive(false);

        // Panel de inventario / cofre
        var invPanel = UIPanel("PanelInventario", canvasGO.transform, new Vector2(1200, 820));
        var invTitle = UIText("Titulo", invPanel, "Inventario", 56, TextAlignmentOptions.Center,
            new Vector2(0.5f, 1), new Vector2(0.5f, 1), new Vector2(0, -20), new Vector2(1000, 90));
        var listRT = UIRect("Lista", invPanel);
        listRT.anchorMin = Vector2.zero; listRT.anchorMax = Vector2.one;
        listRT.offsetMin = new Vector2(40, 140); listRT.offsetMax = new Vector2(-40, -120);
        var vlg = listRT.gameObject.AddComponent<VerticalLayoutGroup>();
        vlg.spacing = 12;
        vlg.childAlignment = TextAnchor.UpperCenter;
        vlg.childControlWidth = true; vlg.childControlHeight = true;
        vlg.childForceExpandWidth = true; vlg.childForceExpandHeight = false;

        var rowTemplate = UIButton("FilaPlantilla", invPanel, mid, mid, Vector2.zero, new Vector2(1000, 90), 36);
        var le = rowTemplate.gameObject.AddComponent<LayoutElement>();
        le.preferredHeight = 90; le.minHeight = 90;
        rowTemplate.gameObject.SetActive(false);

        var bInvClose = UIButton("Cerrar", invPanel, new Vector2(0.5f, 0), new Vector2(0.5f, 0), new Vector2(0, 30), new Vector2(300, 90), 40);

        var invGO = new GameObject("InventarioUI");
        var invUI = invGO.AddComponent<InventoryUI>();
        invUI.panel = invPanel.gameObject;
        invUI.listRoot = listRT;
        invUI.rowTemplate = rowTemplate;
        invUI.title = invTitle;
        invUI.artifact = artifact;
        invUI.party = party;
        UnityEventTools.AddPersistentListener(btnInv.onClick, new UnityAction(invUI.Toggle));
        UnityEventTools.AddPersistentListener(bInvClose.onClick, new UnityAction(invUI.Close));
        invPanel.gameObject.SetActive(false);

        // ---------- Guardar ----------
        string path = comarca ? ComarcaPath : gallery ? GaleriaPath : volcano ? VolcanPath : ScenePath;
        EditorSceneManager.MarkSceneDirty(scene);
        EditorSceneManager.SaveScene(scene, path);

        UpdateBuildSettings();
        AssetDatabase.SaveAssets();

        Debug.Log("Escena creada en " + path);
        if (!quiet) EditorUtility.DisplayDialog("Listo",
            "Escena creada: " + path + "\n\nDale Play.\nWASD o joystick, ESPACIO o boton Saltar, clic derecho + mouse para mirar, clic en los objetos cercanos, tecla I o boton Inventario.", "OK");
    }

    internal static void UpdateBuildSettings()
    {
        var list = new System.Collections.Generic.List<EditorBuildSettingsScene>();
        foreach (var p in new[] { "Assets/Scenes/Hall.unity", MenuPath, ComarcaPath, ScenePath, GaleriaPath })
            if (System.IO.File.Exists(p)) list.Add(new EditorBuildSettingsScene(p, true));
        EditorBuildSettings.scenes = list.ToArray();
    }

    // Pasillo de pruebas con las interacciones (zona de pruebas)
    static LavaCrater BuildCorridor()
    {
        // ---------- Suelo y muros del pasillo de pruebas ----------
        Prim(PrimitiveType.Cube, "Suelo", new Vector3(8, -0.5f, 0), new Vector3(68, 1, 18), Mat("Suelo", new Color(0.35f, 0.45f, 0.3f)));
        var muro = Mat("Muro", new Color(0.4f, 0.4f, 0.45f));
        Prim(PrimitiveType.Cube, "Muro_Izq", new Vector3(8, 1.5f, -9.25f), new Vector3(68, 3, 0.5f), muro);
        Prim(PrimitiveType.Cube, "Muro_Der", new Vector3(8, 1.5f, 9.25f), new Vector3(68, 3, 0.5f), muro);
        Prim(PrimitiveType.Cube, "Muro_Inicio", new Vector3(-26.25f, 1.5f, 0), new Vector3(0.5f, 3, 19), muro);
        // el pasillo queda abierto al final (x = 42) y sigue hacia el volcan

        // ---------- Puertas (cada una cierra todo el ancho) ----------
        Door gateA = Gate("Puerta_A_Maquina", -10f, new Color(0.8f, 0.3f, 0.2f));
        Door gateB = Gate("Puerta_B_Palanca", 2f, new Color(0.3f, 0.5f, 0.8f));
        Door gateC = Gate("Puerta_C_Cristal", 14f, new Color(0.3f, 0.8f, 0.8f));
        Door gateD = Gate("Puerta_D_Placa", 26f, new Color(0.5f, 0.8f, 0.3f));

        // ---------- Estacion A: herramienta -> maquina -> puerta ----------
        var tool = Prim(PrimitiveType.Cube, "Llave inglesa", new Vector3(-18f, 0.4f, 3f), new Vector3(0.9f, 0.25f, 0.25f), Mat("Herramienta", new Color(0.9f, 0.8f, 0.2f), true));
        var toolPickup = tool.AddComponent<ToolPickup>();
        toolPickup.prompt = "Llave inglesa";
        toolPickup.itemName = "Llave inglesa";

        var gem = Prim(PrimitiveType.Sphere, "Gema azul", new Vector3(-17f, 0.4f, -3f), Vector3.one * 0.4f, Mat("Gema", new Color(0.2f, 0.5f, 1f), true));
        var gemPickup = gem.AddComponent<ToolPickup>();
        gemPickup.prompt = "Gema azul";
        gemPickup.itemName = "Gema azul";

        var mRoot = new GameObject("Maquina");
        mRoot.transform.position = new Vector3(-14f, 0, -3f);
        Prim(PrimitiveType.Cube, "Cuerpo", new Vector3(0, 1, 0), new Vector3(2, 2, 2), Mat("MaquinaCuerpo", new Color(0.3f, 0.3f, 0.35f)), mRoot.transform, false);
        var gearPivot = new GameObject("Engranaje");
        gearPivot.transform.SetParent(mRoot.transform, false);
        gearPivot.transform.localPosition = new Vector3(-1.1f, 1.2f, 0);
        gearPivot.transform.localRotation = Quaternion.Euler(0, 90, 0);
        Prim(PrimitiveType.Cube, "Aspa", Vector3.zero, new Vector3(1.4f, 0.25f, 0.12f), Mat("Aspa", new Color(0.9f, 0.5f, 0.1f)), gearPivot.transform, false);
        var mLampGO = new GameObject("Luz");
        mLampGO.transform.SetParent(mRoot.transform, false);
        mLampGO.transform.localPosition = new Vector3(0, 2.6f, 0);
        var mLamp = mLampGO.AddComponent<Light>();
        mLamp.type = LightType.Point; mLamp.range = 9f; mLamp.intensity = 6f; mLamp.color = new Color(1f, 0.6f, 0.2f);
        var mBox = mRoot.AddComponent<BoxCollider>();
        mBox.center = new Vector3(0, 1, 0); mBox.size = new Vector3(2, 2, 2);
        var machine = mRoot.AddComponent<Machine>();
        machine.prompt = "Maquina";
        machine.gear = gearPivot.transform;
        machine.lamp = mLamp;
        machine.targets = new Activatable[] { gateA };

        // ---------- Cofre (guardar objetos y ver como se abre) ----------
        var chestRoot = new GameObject("Cofre");
        chestRoot.transform.position = new Vector3(-20f, 0f, -6f);
        var wood = Mat("Madera", new Color(0.45f, 0.28f, 0.12f));
        var metal = Mat("MetalCofre", new Color(0.7f, 0.6f, 0.2f));
        Prim(PrimitiveType.Cube, "Base", new Vector3(0, 0.4f, 0), new Vector3(1.6f, 0.8f, 1f), wood, chestRoot.transform, false);
        Prim(PrimitiveType.Cube, "Cinta", new Vector3(0, 0.4f, 0), new Vector3(0.2f, 0.82f, 1.02f), metal, chestRoot.transform, false);
        // bisagra en el borde +z; la tapa se extiende hacia -z y al girar se abre hacia arriba
        var lidPivot = new GameObject("Tapa");
        lidPivot.transform.SetParent(chestRoot.transform, false);
        lidPivot.transform.localPosition = new Vector3(0, 0.8f, 0.5f);
        Prim(PrimitiveType.Cube, "TapaMadera", new Vector3(0, 0.15f, -0.5f), new Vector3(1.6f, 0.3f, 1f), wood, lidPivot.transform, false);
        Prim(PrimitiveType.Cube, "TapaCinta", new Vector3(0, 0.16f, -0.5f), new Vector3(0.2f, 0.32f, 1.02f), metal, lidPivot.transform, false);
        var chestBox = chestRoot.AddComponent<BoxCollider>();
        chestBox.center = new Vector3(0, 0.55f, 0); chestBox.size = new Vector3(1.6f, 1.1f, 1f);
        var chest = chestRoot.AddComponent<Chest>();
        chest.prompt = "Cofre";
        chest.lid = lidPivot.transform;

        // ---------- Estacion B: palanca, luz, checkpoint ----------
        var lRoot = new GameObject("Palanca");
        lRoot.transform.position = new Vector3(-5f, 0, -6f);
        Prim(PrimitiveType.Cube, "Base", new Vector3(0, 0.2f, 0), new Vector3(0.8f, 0.4f, 0.8f), Mat("PalancaBase", new Color(0.3f, 0.3f, 0.3f)), lRoot.transform, false);
        var handle = new GameObject("Mango");
        handle.transform.SetParent(lRoot.transform, false);
        handle.transform.localPosition = new Vector3(0, 0.4f, 0);
        var red = Mat("PalancaRoja", new Color(0.85f, 0.15f, 0.15f));
        Prim(PrimitiveType.Cylinder, "Barra", new Vector3(0, 0.5f, 0), new Vector3(0.12f, 0.5f, 0.12f), red, handle.transform, false);
        Prim(PrimitiveType.Sphere, "Perilla", new Vector3(0, 1.0f, 0), Vector3.one * 0.3f, red, handle.transform, false);
        var lBox = lRoot.AddComponent<BoxCollider>();
        lBox.center = new Vector3(0, 0.8f, 0); lBox.size = new Vector3(0.8f, 1.6f, 0.8f);
        var lever = lRoot.AddComponent<Lever>();
        lever.prompt = "Palanca";
        lever.handle = handle.transform;
        lever.targets = new Activatable[] { gateB };

        var sRoot = new GameObject("Interruptor_Luz");
        sRoot.transform.position = new Vector3(-2f, 0, 6f);
        Prim(PrimitiveType.Cube, "Poste", new Vector3(0, 0.75f, 0), new Vector3(0.2f, 1.5f, 0.2f), Mat("Poste", new Color(0.25f, 0.2f, 0.15f)), sRoot.transform, false);
        var bulb = Prim(PrimitiveType.Sphere, "Bombilla", new Vector3(0, 1.7f, 0), Vector3.one * 0.5f, Mat("Bombilla", new Color(1f, 0.9f, 0.6f)), sRoot.transform, false);
        var sLampGO = new GameObject("Luz");
        sLampGO.transform.SetParent(sRoot.transform, false);
        sLampGO.transform.localPosition = new Vector3(0, 2.1f, 0);
        var sLamp = sLampGO.AddComponent<Light>();
        sLamp.type = LightType.Point; sLamp.range = 9f; sLamp.intensity = 6f; sLamp.color = new Color(1f, 0.85f, 0.5f);
        var sBox = sRoot.AddComponent<BoxCollider>();
        sBox.center = new Vector3(0, 1.1f, 0); sBox.size = new Vector3(0.8f, 2.2f, 0.8f);
        var lightSwitch = sRoot.AddComponent<LightSwitch>();
        lightSwitch.prompt = "Luz";
        lightSwitch.lamp = sLamp;
        lightSwitch.bulb = bulb.GetComponent<Renderer>();

        MakeCheckpoint("Checkpoint_1", new Vector3(-7f, 0, 0));

        // ---------- Estacion C: cristal (mago) ----------
        var crystalGO = Prim(PrimitiveType.Cube, "Cristal", new Vector3(8f, 1.5f, 0), new Vector3(1f, 1.6f, 1f), Mat("Cristal", new Color(0.35f, 0.4f, 0.55f)));
        crystalGO.transform.rotation = Quaternion.Euler(0, 45, 0);
        var cGlowGO = new GameObject("Brillo");
        cGlowGO.transform.SetParent(crystalGO.transform, false);
        cGlowGO.transform.localPosition = new Vector3(0, 1.2f, 0);
        var cGlow = cGlowGO.AddComponent<Light>();
        cGlow.type = LightType.Point; cGlow.range = 8f; cGlow.intensity = 6f; cGlow.color = new Color(0.3f, 0.9f, 1f);
        var crystal = crystalGO.AddComponent<Crystal>();
        crystal.glow = cGlow;
        crystal.targets = new Activatable[] { gateC };

        // ---------- Estacion D: placa de presion ----------
        var plate = Prim(PrimitiveType.Cube, "Placa", new Vector3(20f, 0.05f, 0), new Vector3(3f, 0.1f, 3f), Mat("Placa", Color.gray));
        var pBox = plate.GetComponent<BoxCollider>();
        pBox.isTrigger = true; pBox.size = new Vector3(1, 10, 1); pBox.center = new Vector3(0, 4, 0);
        Kinematic(plate);
        var pp = plate.AddComponent<PressurePlate>();
        pp.plate = plate.GetComponent<Renderer>();
        pp.stayPressed = true;
        pp.targets = new Activatable[] { gateD };

        // ---------- Estacion E: gancho (ingeniero) ----------
        Prim(PrimitiveType.Cube, "Plataforma", new Vector3(36f, 1.25f, 0), new Vector3(6f, 2.5f, 8f), Mat("Plataforma", new Color(0.5f, 0.35f, 0.2f)));
        var gp = Prim(PrimitiveType.Sphere, "PuntoGancho", new Vector3(33.2f, 3.4f, 0), Vector3.one * 0.5f, Mat("Gancho", new Color(0.2f, 0.8f, 0.8f), true), null, false);
        var landing = new GameObject("Aterrizaje");
        landing.transform.position = new Vector3(36.5f, 3.6f, 0);
        var grapple = gp.AddComponent<GrapplePoint>();
        grapple.landing = landing.transform;

        // ---------- Zona del volcan ----------
        // Suelo grande que continua el pasillo (x = 42 en adelante)
        Prim(PrimitiveType.Cube, "Suelo_Volcan", new Vector3(92f, -0.5f, 0), new Vector3(100, 1, 100), Mat("SueloVolcan", new Color(0.3f, 0.28f, 0.25f)));
        MakeCheckpoint("Checkpoint_Volcan", new Vector3(54f, 0, 0));

        // Montana en terrazas de 1 m (se sube saltando). Cada terraza es un cilindro con collider de malla.
        Vector3 vc = new Vector3(95f, 0f, 0f);
        const int tiers = 8;
        for (int i = 0; i < tiers; i++)
        {
            float radius = 34f - 3.6f * i;
            float h = i + 1f;
            float k = i / (float)(tiers - 1);
            Color c = Color.Lerp(new Color(0.32f, 0.38f, 0.25f), new Color(0.15f, 0.12f, 0.12f), k);
            Cylinder("Terraza_" + (i + 1), vc + new Vector3(0, h * 0.5f, 0), radius, h, Mat("Terraza_" + (i + 1), c));
        }
        float topY = tiers;   // 8 m

        // Crater con lava en la cima: aqui se arroja el artefacto
        var lava = Cylinder("Lava", vc + new Vector3(0, topY + 0.05f, 0), 3.5f, 0.1f, Mat("Lava", new Color(1f, 0.35f, 0.05f), true));
        var lavaLightGO = new GameObject("LuzLava");
        lavaLightGO.transform.position = vc + new Vector3(0, topY + 2.5f, 0);
        var lavaLight = lavaLightGO.AddComponent<Light>();
        lavaLight.type = LightType.Point; lavaLight.range = 25f; lavaLight.intensity = 12f; lavaLight.color = new Color(1f, 0.45f, 0.1f);
        var crater = lava.AddComponent<LavaCrater>();
        crater.prompt = "Crater";
        crater.interactDistance = 11f;

        // ---------- Zona de caida ----------
        var kz = Prim(PrimitiveType.Cube, "ZonaCaida", new Vector3(50f, -12f, 0), new Vector3(400f, 1f, 300f), Mat("Caida", Color.black));
        kz.GetComponent<BoxCollider>().isTrigger = true;
        kz.GetComponent<Renderer>().enabled = false;
        Kinematic(kz);
        kz.AddComponent<KillZone>();

        // ---------- Carteles ----------
        Label("A  Cadena: Herramienta > Maquina > Puerta", new Vector3(-16f, 4.5f, 0));
        Label("Cofre: guarda y saca objetos", new Vector3(-20f, 3.5f, -6f));
        Label("B  Palanca, luz y checkpoint", new Vector3(-4f, 4.5f, 0));
        Label("C  Mago: dispara al cristal", new Vector3(8f, 5f, 0));
        Label("D  Placa de presion", new Vector3(20f, 5f, 0));
        Label("E  Ingeniero: gancho", new Vector3(31f, 6.5f, 0));
        Label("VOLCAN: sube saltando y arroja el artefacto", new Vector3(62f, 5f, 0));

        return crater;
    }

    // ------------------------------------------------------------------ Helpers 3D
    internal static void EnsureFolder(string parent, string name)
    {
        if (!AssetDatabase.IsValidFolder(parent + "/" + name))
            AssetDatabase.CreateFolder(parent, name);
    }

    internal static Material Mat(string name, Color c, bool emissive = false)
    {
        string path = "Assets/Materials/" + name + ".mat";
        var m = AssetDatabase.LoadAssetAtPath<Material>(path);
        if (m == null)
        {
            var sh = Shader.Find("Universal Render Pipeline/Lit");
            if (sh == null) sh = Shader.Find("Standard");
            m = new Material(sh);
            AssetDatabase.CreateAsset(m, path);
        }
        m.color = c;
        if (m.HasProperty("_BaseColor")) m.SetColor("_BaseColor", c);
        if (emissive)
        {
            m.EnableKeyword("_EMISSION");
            m.SetColor("_EmissionColor", c * 1.5f);
        }
        EditorUtility.SetDirty(m);
        return m;
    }

    internal static GameObject Prim(PrimitiveType type, string name, Vector3 pos, Vector3 scale, Material mat,
                           Transform parent = null, bool keepCollider = true)
    {
        var go = GameObject.CreatePrimitive(type);
        go.name = name;
        if (parent != null) go.transform.SetParent(parent, false);
        go.transform.localPosition = pos;
        go.transform.localScale = scale;
        go.GetComponent<Renderer>().sharedMaterial = mat;
        if (!keepCollider) Object.DestroyImmediate(go.GetComponent<Collider>());
        return go;
    }

    // Cilindro con collider de malla (el de capsula que trae Unity no sirve para caminar encima)
    internal static GameObject Cylinder(string name, Vector3 center, float radius, float height, Material mat)
    {
        var go = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
        go.name = name;
        go.transform.position = center;
        go.transform.localScale = new Vector3(radius * 2f, height * 0.5f, radius * 2f);
        go.GetComponent<Renderer>().sharedMaterial = mat;
        Object.DestroyImmediate(go.GetComponent<Collider>());
        var mc = go.AddComponent<MeshCollider>();
        mc.sharedMesh = go.GetComponent<MeshFilter>().sharedMesh;
        return go;
    }

    internal static Door Gate(string name, float x, Color c)
    {
        var go = Prim(PrimitiveType.Cube, name, new Vector3(x, 2f, 0), new Vector3(0.6f, 4f, 18f), Mat(name, c));
        return go.AddComponent<Door>();
    }

    internal static void Kinematic(GameObject go)
    {
        var rb = go.AddComponent<Rigidbody>();
        rb.isKinematic = true;
        rb.useGravity = false;
    }

    internal static void MakeCheckpoint(string name, Vector3 pos)
    {
        var cRoot = new GameObject(name);
        cRoot.transform.position = pos;
        Prim(PrimitiveType.Cube, "Asta", new Vector3(0, 1, 0), new Vector3(0.2f, 2f, 0.2f), Mat("Asta", Color.white), cRoot.transform, false);
        var flag = Prim(PrimitiveType.Cube, "Bandera", new Vector3(0.4f, 1.75f, 0), new Vector3(0.8f, 0.5f, 0.05f), Mat("Bandera", new Color(0.6f, 0.6f, 0.6f)), cRoot.transform, false);
        var cBox = cRoot.AddComponent<BoxCollider>();
        cBox.isTrigger = true; cBox.center = new Vector3(0, 1.5f, 0); cBox.size = new Vector3(3, 3, 3);
        Kinematic(cRoot);
        var checkpoint = cRoot.AddComponent<Checkpoint>();
        checkpoint.flag = flag.GetComponent<Renderer>();
    }

    internal static PlayerMovement MakeChar(string name, Role role, Vector3 pos, Color col, float scale)
    {
        var go = GameObject.CreatePrimitive(PrimitiveType.Capsule);
        go.name = name;
        Object.DestroyImmediate(go.GetComponent<Collider>());
        go.transform.position = pos;
        go.transform.localScale = Vector3.one * scale;
        go.GetComponent<Renderer>().sharedMaterial = Mat("Pj_" + name, col);

        var cc = go.AddComponent<CharacterController>();
        cc.height = 2f; cc.radius = 0.5f; cc.center = Vector3.zero;

        var pm = go.AddComponent<PlayerMovement>();
        var r = go.AddComponent<CharacterRole>();
        r.role = role;

        // modelo con textura (Fantasy Races + animaciones UAL); si no esta disponible queda la capsula
        string race = role == Role.Hobbit ? "Elf_Female" : role == Role.Mage ? "Elf_Male" : role == Role.Engineer ? "Orc_Female" : "Orc_Male";
        int variant = role == Role.Hobbit ? 3 : role == Role.Mage ? 2 : role == Role.Engineer ? 2 : 3;
        GameObject body = RacesKit.Available ? RacesKit.Spawn(go.transform, "Modelo", race, new Vector3(0f, -1f, 0f), 0f, variant, "Idle_Loop", false) : null;
        if (body != null)
        {
            body.AddComponent<PjAnim>();
            go.GetComponent<Renderer>().enabled = false;
        }
        else
            Prim(PrimitiveType.Sphere, "Frente", new Vector3(0, 0.5f, 0.5f), Vector3.one * 0.35f, Mat("Nariz", Color.white), go.transform, false);
        return pm;
    }

    internal static GameObject Label(string text, Vector3 pos)
    {
        var go = new GameObject("Cartel");
        go.transform.position = pos;
        go.transform.rotation = Quaternion.Euler(0, 90, 0);   // legible mirando hacia +X
        var t = go.AddComponent<TextMeshPro>();
        t.text = text;
        t.fontSize = 5f;
        t.alignment = TextAlignmentOptions.Center;
        t.color = Color.white;
        t.rectTransform.sizeDelta = new Vector2(14, 3);
        return go;
    }

    // ------------------------------------------------------------------ Helpers UI
    internal static RectTransform UIRect(string name, Transform parent)
    {
        var go = new GameObject(name, typeof(RectTransform));
        go.transform.SetParent(parent, false);
        return go.GetComponent<RectTransform>();
    }

    internal static void SetRect(RectTransform rt, Vector2 anchor, Vector2 pivot, Vector2 pos, Vector2 size)
    {
        rt.anchorMin = anchor; rt.anchorMax = anchor;
        rt.pivot = pivot;
        rt.anchoredPosition = pos;
        rt.sizeDelta = size;
    }

    internal static RectTransform UIPanel(string name, Transform parent, Vector2 size)
    {
        var rt = UIRect(name, parent);
        SetRect(rt, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), Vector2.zero, size);
        var img = rt.gameObject.AddComponent<Image>();
        img.color = new Color(0.07f, 0.07f, 0.12f, 0.92f);
        return rt;
    }

    internal static TextMeshProUGUI UIText(string name, Transform parent, string text, int size, TextAlignmentOptions align,
                                  Vector2 anchor, Vector2 pivot, Vector2 pos, Vector2 sizeDelta)
    {
        var rt = UIRect(name, parent);
        SetRect(rt, anchor, pivot, pos, sizeDelta);
        var t = rt.gameObject.AddComponent<TextMeshProUGUI>();
        t.text = text;
        t.fontSize = size;
        t.alignment = align;
        t.color = Color.white;
        t.raycastTarget = false;
        return t;
    }

    internal static Button UIButton(string label, Transform parent, Vector2 anchor, Vector2 pivot, Vector2 pos, Vector2 size, int fontSize)
    {
        var go = TMP_DefaultControls.CreateButton(new TMP_DefaultControls.Resources());
        go.name = "Boton_" + label;
        go.transform.SetParent(parent, false);
        SetRect((RectTransform)go.transform, anchor, pivot, pos, size);

        var img = go.GetComponent<Image>();
        img.sprite = AssetDatabase.GetBuiltinExtraResource<Sprite>("UI/Skin/UISprite.psd");
        img.type = Image.Type.Sliced;
        img.color = new Color(0.18f, 0.18f, 0.3f, 0.9f);

        var txt = go.GetComponentInChildren<TMP_Text>();
        txt.text = label;
        txt.fontSize = fontSize;
        txt.color = Color.white;
        return go.GetComponent<Button>();
    }
}
