using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

// Mini tutoriales de primera vez (se guardan en PlayerPrefs) + carteles que solo aparecen al acercarse.
public static class Tutorial
{
    static GameObject window;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    static void ResetStatics() => window = null;

    public static bool Seen(string key) => PlayerPrefs.GetInt("tuto_" + key, 0) == 1;

    // Muestra el tutorial solo la primera vez; luego (o si ya se vio) ejecuta "after" directamente.
    public static void Show(string key, string title, string body, Action after = null)
    {
        if (Seen(key) || window != null) { after?.Invoke(); return; }
        PlayerPrefs.SetInt("tuto_" + key, 1);
        PlayerPrefs.Save();

        Modal.Begin();
        var panel = UiKit.Panel(title, new Vector2(1250, 760), out var content);
        window = panel.gameObject;

        var tgo = new GameObject("Texto", typeof(RectTransform), typeof(LayoutElement));
        tgo.transform.SetParent(content, false);
        tgo.GetComponent<LayoutElement>().preferredHeight = 420f;
        var t = tgo.AddComponent<TextMeshProUGUI>();
        t.text = body;
        t.fontSize = 40;
        t.alignment = TextAlignmentOptions.Center;
        t.color = Color.white;
        t.textWrappingMode = TextWrappingModes.Normal;
        t.raycastTarget = false;

        UiKit.Btn(content, "Entendido", 110f, () =>
        {
            UnityEngine.Object.Destroy(window);
            window = null;
            Modal.End();
            after?.Invoke();
        }, 46);
    }
}

// Texto 3D que se ve solo cuando el jugador esta cerca y siempre mira hacia la camara.
public class ProximityLabel : MonoBehaviour
{
    public float radius = 14f;
    TMP_Text t;
    PartyManager party;

    void Awake()
    {
        t = GetComponent<TMP_Text>();
        if (t != null) { t.alpha = 0f; t.enabled = false; }
    }

    void Update()
    {
        if (t == null) return;
        if (party == null) party = FindFirstObjectByType<PartyManager>();
        var cam = Camera.main;
        if (party == null || party.Current == null || cam == null) return;

        float d = Vector3.Distance(party.Current.transform.position, transform.position);
        float target = d < radius ? 1f : 0f;
        if (target > 0f) t.enabled = true;
        t.alpha = Mathf.MoveTowards(t.alpha, target, Time.unscaledDeltaTime * 3f);
        if (t.alpha < 0.01f) t.enabled = false;

        Vector3 dir = transform.position - cam.transform.position;
        dir.y = 0f;
        if (dir.sqrMagnitude > 0.01f) transform.rotation = Quaternion.LookRotation(dir);
    }
}
