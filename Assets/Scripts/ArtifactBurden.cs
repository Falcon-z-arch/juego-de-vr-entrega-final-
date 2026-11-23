using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

// "Carga del artefacto": quien lo lleva se va corrompiendo. Los otros personajes se recuperan.
// Hay que rotar de personaje para aguantar, y cada uno resiste distinto:
//   Hobbit (resistente) sube lento, Mago e Ingeniero suben mas rapido.
// Con 70% o mas vas lento. Al llegar a 100% el artefacto te vence: golpe, y relevo forzado al mas descansado.
// Un personaje agotado no se puede elegir hasta bajar de 50%.
// Al relevar, el nuevo personaje recibe unos segundos de impulso (mas velocidad).
public class ArtifactBurden : MonoBehaviour
{
    public PartyManager party;

    public float hobbitRate = 1.2f;       // % por segundo llevando el artefacto
    public float otherRate = 2.2f;
    public float recoverRate = 4f;        // % por segundo de los que descansan
    public float lockUntil = 50f;         // un agotado vuelve a estar disponible bajo este valor
    public float boostTime = 4f;
    public float boostSpeed = 1.3f;

    float[] meter;
    bool[] exhausted;
    int lastIndex = -1;
    float boostUntil;
    TMP_Text label;

    void Start()
    {
        if (party == null) party = GetComponent<PartyManager>();
        meter = new float[party.characters.Length];
        exhausted = new bool[party.characters.Length];
        MakeLabel();
    }

    public bool CanSwitchTo(int i)
    {
        if (exhausted == null || i < 0 || i >= exhausted.Length) return true;
        if (exhausted[i])
        {
            HUD.Say(party.characters[i].name + " esta agotado (" + (int)meter[i] + "%). Espera a que baje de " + (int)lockUntil + "%");
            return false;
        }
        return true;
    }

    void Update()
    {
        if (meter == null || party == null) return;

        int cur = party.CurrentIndex;
        if (cur < 0) return;

        if (cur != lastIndex)
        {
            if (lastIndex >= 0)
            {
                boostUntil = Time.time + boostTime + RunState.BoostTimeAdd;
                HUD.Say("Relevo! Impulso por " + (int)boostTime + " s");
            }
            lastIndex = cur;
        }

        bool inHub = !RunState.InRun;   // en la aldea no hay carga
        bool carried = !inHub && party.artifact != null && party.artifact.IsCarried && !party.artifact.IsGone;

        for (int i = 0; i < meter.Length; i++)
        {
            if (i == cur && carried)
            {
                var r = party.characters[i].GetComponent<CharacterRole>();
                float rate = ((r != null && r.role == Role.Hobbit) ? hobbitRate : otherRate) * RunState.BurdenMul;
                meter[i] = Mathf.Min(100f, meter[i] + rate * Time.deltaTime);
            }
            else
            {
                meter[i] = Mathf.Max(0f, meter[i] - recoverRate * RunState.RecoverMul * Time.deltaTime);
            }
            if (exhausted[i] && meter[i] < lockUntil) exhausted[i] = false;
        }

        // efectos sobre el personaje activo
        float mul = 1f;
        if (meter[cur] >= 70f) mul = 0.75f;
        if (Time.time < boostUntil) mul = boostSpeed;
        for (int i = 0; i < party.characters.Length; i++)
            party.characters[i].speedMul = (i == cur) ? mul * RunState.SpeedMul : 1f;

        // se vence
        if (meter[cur] >= 100f) Overwhelm(cur);

        UpdateLabel(cur);
    }

    void Overwhelm(int cur)
    {
        exhausted[cur] = true;

        int best = -1;
        float bv = float.MaxValue;
        for (int i = 0; i < meter.Length; i++)
        {
            if (i == cur || exhausted[i] || !party.IsUsable(i)) continue;
            if (meter[i] < bv) { bv = meter[i]; best = i; }
        }

        HUD.Say("El artefacto te vencio! Relevo forzado");
        Sfx.Beep(140f, 0.4f);
        if (best >= 0) party.ForceSelect(best);
        else meter[cur] = 90f;   // todos agotados: sigues, pero al limite
        if (best < 0) exhausted[cur] = false;
    }

    // ---------- texto en pantalla (se crea solo, sin tocar el Canvas del builder) ----------
    void MakeLabel()
    {
        // evita canvas temporales (cinematica, menu de vendedor, boton hablar): al destruirse se llevarian el texto
        Canvas canvas = null;
        foreach (var c in FindObjectsByType<Canvas>(FindObjectsSortMode.None))
        {
            if (c.transform.parent != null || !c.isRootCanvas) continue;
            string n = c.name;
            if (n.StartsWith("Cinematica") || n.StartsWith("MenuVendedor") || n.StartsWith("CanvasHablar") || n.StartsWith("Overlay")) continue;
            canvas = c; break;
        }
        if (canvas == null) return;

        var go = new GameObject("CargaArtefactoText", typeof(RectTransform));
        go.transform.SetParent(canvas.transform, false);
        var rt = (RectTransform)go.transform;
        rt.anchorMin = rt.anchorMax = new Vector2(0f, 1f);
        rt.pivot = new Vector2(0f, 1f);
        rt.anchoredPosition = new Vector2(40f, -110f);
        rt.sizeDelta = new Vector2(900f, 120f);

        var t = go.AddComponent<TextMeshProUGUI>();
        t.fontSize = 34;
        t.color = Color.white;
        t.raycastTarget = false;
        t.alignment = TextAlignmentOptions.TopLeft;
        label = t;
    }

    static string Bar(float v)
    {
        int n = Mathf.RoundToInt(v / 10f);
        return new string('#', n) + new string('-', 10 - n);
    }

    void UpdateLabel(int cur)
    {
        if (label == null) { MakeLabel(); if (label == null) return; }
        var sb = new System.Text.StringBuilder("CARGA DEL ARTEFACTO\n");
        for (int i = 0; i < meter.Length; i++)
        {
            string mark = i == cur ? ">" : " ";
            string state = exhausted[i] ? " AGOTADO" : "";
            sb.Append(mark).Append(party.characters[i].name).Append(' ').Append(Bar(meter[i])).Append(' ').Append((int)meter[i]).Append('%').Append(state).Append('\n');
        }
        label.text = sb.ToString();
    }
}
