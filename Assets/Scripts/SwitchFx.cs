using System.Collections;
using UnityEngine;
using UnityEngine.UI;

// Animacion de cambio de personaje: destello de pantalla del color del personaje, luz, chispas que suben y un pequeno zoom.
public static class SwitchFx
{
    public static void Play(Vector3 pos, Color c)
    {
        var go = new GameObject("CambioFx");
        go.AddComponent<SwitchFxRunner>().Run(pos, c);
    }
}

public class SwitchFxRunner : MonoBehaviour
{
    public void Run(Vector3 pos, Color c) => StartCoroutine(Go(pos, c));

    IEnumerator Go(Vector3 pos, Color c)
    {
        // destello de pantalla
        Image flash = null;
        var canvas = UiKit.MainCanvas();
        if (canvas != null)
        {
            var g = new GameObject("FlashCambio", typeof(RectTransform), typeof(Image));
            g.transform.SetParent(canvas.transform, false);
            g.transform.SetAsLastSibling();
            var rt = (RectTransform)g.transform;
            rt.anchorMin = Vector2.zero; rt.anchorMax = Vector2.one; rt.offsetMin = rt.offsetMax = Vector2.zero;
            flash = g.GetComponent<Image>();
            flash.raycastTarget = false;
        }

        // luz
        var lg = new GameObject("LuzCambio");
        lg.transform.position = pos + Vector3.up;
        var l = lg.AddComponent<Light>();
        l.type = LightType.Point; l.range = 12f; l.color = c; l.intensity = 40f;

        // chispas
        const int N = 16;
        var sp = new Transform[N];
        var vel = new Vector3[N];
        for (int i = 0; i < N; i++)
        {
            var s = GameObject.CreatePrimitive(PrimitiveType.Cube);
            Object.Destroy(s.GetComponent<Collider>());
            s.GetComponent<Renderer>().material.color = c;
            s.transform.position = pos + new Vector3(Random.Range(-0.4f, 0.4f), Random.Range(0f, 1.6f), Random.Range(-0.4f, 0.4f));
            s.transform.localScale = Vector3.one * Random.Range(0.08f, 0.18f);
            sp[i] = s.transform;
            vel[i] = new Vector3(Random.Range(-1.2f, 1.2f), Random.Range(2f, 5f), Random.Range(-1.2f, 1.2f));
        }

        var cam = Camera.main;
        float baseFov = cam != null ? cam.fieldOfView : 60f;
        bool kick = cam != null && !VRRig.Active;

        float t = 0f, dur = 0.7f;
        while (t < dur)
        {
            t += Time.unscaledDeltaTime;
            float k = t / dur;
            if (flash != null) flash.color = new Color(c.r, c.g, c.b, Mathf.Lerp(0.6f, 0f, Mathf.Clamp01(t / 0.45f)));
            l.intensity = Mathf.Lerp(40f, 0f, k);
            for (int i = 0; i < N; i++)
            {
                if (sp[i] == null) continue;
                sp[i].position += vel[i] * Time.unscaledDeltaTime;
                sp[i].localScale *= 1f - 1.6f * Time.unscaledDeltaTime;
            }
            if (kick && cam != null) cam.fieldOfView = baseFov + 14f * Mathf.Sin(Mathf.Clamp01(t / 0.4f) * Mathf.PI);
            yield return null;
        }
        if (kick && cam != null) cam.fieldOfView = baseFov;
        if (flash != null) Destroy(flash.gameObject);
        Destroy(lg);
        foreach (var s in sp) if (s != null) Destroy(s.gameObject);
        Destroy(gameObject);
    }
}
