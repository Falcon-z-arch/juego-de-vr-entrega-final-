using System.Collections;
using UnityEngine;
using UnityEngine.InputSystem;

// Cinematica final: el Ascua cae al crater, erupcion, destello y tarjetas de epilogo.
// Despues se guarda el botin y se vuelve a la Comarca.
public class EndingCinematic : MonoBehaviour
{
    static bool playing;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    static void ResetStatics() => playing = false;

    static readonly string[] Cards =
    {
        "El Ascua toca el fuego de Karnak.\nEl crater ruge... y el reloj del mundo se detiene.",
        "Un segundo.\n\nDos.\n\nY entonces, el tiempo avanza.",
        "Por primera vez en mil años, el amanecer llega a Valdenar.\nLos viajeros vuelven a la Comarca, y nadie recuerda que el tiempo alguna vez se repitio."
    };

    public static void Play()
    {
        if (playing) return;
        playing = true;
        new GameObject("CinematicaFinal").AddComponent<EndingCinematic>();
    }

    void Start() => StartCoroutine(Run());

    IEnumerator Run()
    {
        // se apagan los peligros
        var sp = FindFirstObjectByType<FallingRockSpawner>();
        if (sp != null) sp.enabled = false;
        var dr = FindFirstObjectByType<AvalancheDirector>();
        if (dr != null) dr.enabled = false;
        foreach (var r in FindObjectsByType<FallingRock>(FindObjectsSortMode.None)) Destroy(r.gameObject);

        // sin musica: solo queda el viento (WindAmbience)

        // erupcion + temblor
        var crater = FindFirstObjectByType<LavaCrater>();
        Vector3 origin = crater != null ? crater.transform.position : Camera.main.transform.position + Camera.main.transform.forward * 8f;
        var cam = FindFirstObjectByType<CameraFollow>();
        var sparks = new System.Collections.Generic.List<GameObject>();
        for (int i = 0; i < 40; i++)
        {
            var s = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            Destroy(s.GetComponent<Collider>());
            s.name = "Chispa";
            s.transform.position = origin + Random.insideUnitSphere * 1.5f;
            s.transform.localScale = Vector3.one * Random.Range(0.2f, 0.7f);
            s.GetComponent<Renderer>().material.color = Color.Lerp(new Color(1f, 0.3f, 0.05f), new Color(1f, 0.9f, 0.3f), Random.value);
            var rb = s.AddComponent<Rigidbody>();
            rb.linearVelocity = new Vector3(Random.Range(-6f, 6f), Random.Range(14f, 30f), Random.Range(-6f, 6f));
            Destroy(s, 6f);
            sparks.Add(s);
        }

        float t = 0f;
        while (t < 2.6f)
        {
            t += Time.deltaTime;
            if (cam != null) { cam.yaw += (Random.value - 0.5f) * 1.2f; cam.pitch += (Random.value - 0.5f) * 0.8f; }
            if (Random.value < 0.15f) Sfx.Beep(Random.Range(90f, 160f), 0.2f);
            yield return null;
        }

        // destello blanco y silencio
        var flash = Overlay.Create("", Color.white);
        yield return flash.FadeIn(0.9f);
        Time.timeScale = 0f;

        // tarjetas
        for (int i = 0; i < Cards.Length; i++)
        {
            var ov = Overlay.Create(Cards[i], Color.black, 50);
            yield return ov.FadeIn(0.9f);
            float w = 0f;
            float need = i == 1 ? 4f : 5.5f;
            bool skip = false;
            while (w < need && !skip)
            {
                float prev = w;
                w += Time.unscaledDeltaTime;
                if (i == 1 && (int)(w * 1.5f) != (int)(prev * 1.5f)) Sfx.Beep(w < 3f ? 800f : 400f, 0.06f);
                if (Pressed()) skip = true;
                yield return null;
            }
            yield return ov.FadeOut(0.6f);
            ov.Destroy();
        }
        flash.Destroy();

        // botin y fin
        int gained = 0;
        if (RunManager.I != null) gained = RunManager.I.Win();
        var end = Overlay.Create("FIN\n\nGracias por jugar\nBotin de la victoria: +" + gained + " monedas\n\n(toca para volver)", Color.black, 64);
        yield return end.FadeIn(1.2f);
        while (!Pressed()) yield return null;

        end.Destroy();
        MusicPlayer.Stop();
        playing = false;
        RunManager.GoHome();
    }

    static bool Pressed()
    {
        var p = Pointer.current;
        if (p != null && p.press.wasPressedThisFrame) return true;
        var k = Keyboard.current;
        return k != null && (k.spaceKey.wasPressedThisFrame || k.enterKey.wasPressedThisFrame);
    }
}
