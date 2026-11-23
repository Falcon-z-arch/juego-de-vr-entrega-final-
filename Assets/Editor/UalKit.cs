using System.IO;
using UnityEditor;
using UnityEditor.Animations;
using UnityEngine;
using static ZonaPruebasBuilder;

// Universal Animation Library (Quaternius, CC0): maniqui humanoide + 43 animaciones (Idle, Walk, Jog, Sprint, Jump, Sword, Spell, Sitting...).
// Archivo esperado: Assets/Modelos3D/Animaciones/UAL1_Standard.fbx (version sin root motion).
// Se importa como Humanoid, se crea un controlador con un estado por animacion (nombre = nombre del clip)
// y ActorAnim los reproduce por nombre.
internal static class UalKit
{
    const string Fbx = "Assets/Modelos3D/Animaciones/UAL1_Standard.fbx";
    const string CtrlDir = "Assets/Resources/Animaciones";
    const string Ctrl = CtrlDir + "/UAL.controller";

    internal static bool Available => AssetDatabase.LoadAssetAtPath<GameObject>(Fbx) != null;

    [MenuItem("Certamen2/8 - Preparar animaciones (UAL)")]
    internal static void Prepare()
    {
        if (!File.Exists(Fbx)) return;
        var imp = AssetImporter.GetAtPath(Fbx) as ModelImporter;
        if (imp == null) return;

        // 1) rig humanoide
        if (imp.animationType != ModelImporterAnimationType.Human || !imp.bakeAxisConversion)
        {
            imp.bakeAxisConversion = true;
            imp.animationType = ModelImporterAnimationType.Human;
            imp.avatarSetup = ModelImporterAvatarSetup.CreateFromThisModel;
            imp.SaveAndReimport();
        }

        // 2) clips: loop en los *_Loop, root bloqueado (se mueve por codigo), material del maniqui
        imp = AssetImporter.GetAtPath(Fbx) as ModelImporter;
        imp.importAnimation = true;
        imp.motionNodeName = "root";
        var clips = imp.defaultClipAnimations;
        foreach (var c in clips)
        {
            string cn = c.name.Substring(c.name.LastIndexOf('|') + 1);
            bool loop = cn.EndsWith("_Loop") || cn.Contains("Idle") || cn == "A_TPose";
            c.loopTime = loop;
            c.loopPose = loop;
            c.lockRootRotation = true;
            c.lockRootHeightY = true;
            c.lockRootPositionXZ = true;
        }
        imp.clipAnimations = clips;
        EnsureFolder("Assets/Modelos3D", "Animaciones");
        var baseMat = BodyMat(new Color(0.75f, 0.72f, 0.66f), "UAL_Base");
        foreach (var o in AssetDatabase.LoadAllAssetsAtPath(Fbx))
            if (o is Material m)
                imp.AddRemap(new AssetImporter.SourceAssetIdentifier(typeof(Material), m.name), baseMat);
        imp.SaveAndReimport();

        // 3) controlador con un estado por clip
        EnsureFolder("Assets", "Resources");
        EnsureFolder("Assets/Resources", "Animaciones");
        AssetDatabase.DeleteAsset(Ctrl);
        var ctrl = AnimatorController.CreateAnimatorControllerAtPath(Ctrl);
        var sm = ctrl.layers[0].stateMachine;
        AnimatorState idle = null;
        int n = 0;
        foreach (var o in AssetDatabase.LoadAllAssetsAtPath(Fbx))
        {
            if (!(o is AnimationClip clip) || clip.name.StartsWith("__preview__")) continue;
            string sn = clip.name.Substring(clip.name.LastIndexOf('|') + 1);   // "Armature|Idle_Loop" -> "Idle_Loop"
            var st = sm.AddState(sn);
            st.motion = clip;
            if (sn == "Idle_Loop") idle = st;
            n++;
        }
        if (idle != null) sm.defaultState = idle;
        EditorUtility.SetDirty(ctrl);
        AssetDatabase.SaveAssets();
        Debug.Log("UAL: " + n + " animaciones en el controlador.");
    }

    static Material BodyMat(Color c, string name)
    {
        EnsureFolder("Assets/Modelos3D/Animaciones", "Materials");
        string path = "Assets/Modelos3D/Animaciones/Materials/" + name + ".mat";
        var m = AssetDatabase.LoadAssetAtPath<Material>(path);
        if (m == null)
        {
            m = new Material(Shader.Find("Universal Render Pipeline/Lit") ?? Shader.Find("Standard")) { name = name };
            AssetDatabase.CreateAsset(m, path);
        }
        m.color = c;
        if (m.HasProperty("_BaseColor")) m.SetColor("_BaseColor", c);
        m.SetFloat("_Smoothness", 0.2f);
        EditorUtility.SetDirty(m);
        return m;
    }

    // Crea un personaje animado. pos/yaw locales al padre. Devuelve el objeto (con ActorAnim) o null.
    internal static GameObject Spawn(Transform parent, string name, Vector3 localPos, float yaw, Color shirt, string idle, bool collider, float scale = 1f)
    {
        var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(Fbx);
        var ctrl = AssetDatabase.LoadAssetAtPath<RuntimeAnimatorController>(Ctrl);
        if (prefab == null || ctrl == null) return null;

        var holder = new GameObject(name);
        holder.transform.SetParent(parent, false);
        holder.transform.localPosition = localPos;
        holder.transform.localRotation = Quaternion.Euler(0f, yaw, 0f);
        holder.transform.localScale = Vector3.one * scale;
        var go = (GameObject)PrefabUtility.InstantiatePrefab(prefab);
        go.name = "Cuerpo";
        go.transform.SetParent(holder.transform, false);
        go.transform.localPosition = Vector3.zero;
        go.transform.localRotation = Quaternion.identity;

        var an = go.GetComponent<Animator>();
        if (an == null) an = go.AddComponent<Animator>();
        an.runtimeAnimatorController = ctrl;
        an.applyRootMotion = false;

        var mat = BodyMat(shirt, "UAL_" + ColorUtility.ToHtmlStringRGB(shirt));
        foreach (var r in go.GetComponentsInChildren<Renderer>())
        {
            var arr = new Material[Mathf.Max(1, r.sharedMaterials.Length)];
            for (int i = 0; i < arr.Length; i++) arr[i] = mat;
            r.sharedMaterials = arr;
        }
        var aa = holder.AddComponent<ActorAnim>();
        aa.idleClip = idle;
        if (collider)
        {
            var cc = holder.AddComponent<CapsuleCollider>();
            cc.radius = 0.4f; cc.height = 1.8f; cc.center = new Vector3(0f, 0.9f, 0f);
        }
        return holder;
    }

    // Aldeanos de la Comarca: elfos (si esta el kit de razas; si no, maniquies). Dos pasean, dos conversan, dos orcos vigilan el paso a la montana.
    internal static void Villagers()
    {
        if (!Available) return;
        var root = new GameObject("Aldeanos").transform;
        bool races = RacesKit.Available;
        GameObject Make(string n, string race, int v, Vector3 pos, float yaw, Color col, string idle)
            => races ? RacesKit.Spawn(root, n, race, pos, yaw, v, idle, false) : Spawn(root, n, pos, yaw, col, idle, false);

        var a = Make("AldeanoPaseo1", "Elf_Female", 1, new Vector3(-36f, 0f, 2.5f), 90f, new Color(0.55f, 0.38f, 0.22f), "Idle_Loop");
        if (a != null) { var aa = a.GetComponent<ActorAnim>(); aa.route = new[] { new Vector3(-36f, 0f, 2.5f), new Vector3(-14f, 0f, 2.5f) }; aa.walkClip = "Walk_Loop"; aa.speed = 1.1f; }
        var b = Make("AldeanoPaseo2", "Elf_Male", 2, new Vector3(8f, 0f, -2.5f), 90f, new Color(0.45f, 0.3f, 0.55f), "Idle_Loop");
        if (b != null) { var ab = b.GetComponent<ActorAnim>(); ab.route = new[] { new Vector3(8f, 0f, -2.5f), new Vector3(30f, 0f, -2.5f) }; ab.walkClip = "Walk_Formal_Loop"; ab.speed = 1.0f; ab.pauseTime = 6f; }
        Make("AldeanoCharla1", "Elf_Male", 3, new Vector3(-6f, 0f, 3.4f), 90f, new Color(0.3f, 0.5f, 0.3f), "Idle_Talking_Loop");
        Make("AldeanoCharla2", "Elf_Female", 2, new Vector3(-4.6f, 0f, 3.4f), 270f, new Color(0.6f, 0.3f, 0.3f), "Idle_Talking_Loop");
        if (races)
        {
            Make("GuardiaOrco1", "Orc_Male", 1, new Vector3(68f, 0f, 4.5f), 270f, Color.gray, "Sword_Idle");
            Make("GuardiaOrco2", "Orc_Female", 1, new Vector3(68f, 0f, -4.5f), 270f, Color.gray, "Sword_Idle");
        }
    }
}
