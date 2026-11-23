using UnityEngine;

// Retroalimentacion sonora. Genera tonos por codigo, asi no necesitas archivos de audio.
// Si en un objeto asignas un AudioClip, se usa ese en lugar del tono.
public static class Sfx
{
    static AudioSource src;

    public static void Beep(float freq = 660f, float dur = 0.15f, float vol = 0.5f)
    {
        EnsureSource();
        int rate = 44100;
        int n = Mathf.Max(1, (int)(rate * dur));
        var data = new float[n];
        for (int i = 0; i < n; i++)
        {
            float t = i / (float)rate;
            float env = 1f - (i / (float)n);
            data[i] = Mathf.Sin(2f * Mathf.PI * freq * t) * env;
        }
        var clip = AudioClip.Create("beep", n, 1, rate, false);
        clip.SetData(data, 0);
        src.PlayOneShot(clip, vol);
    }

    public static void Ping(AudioClip clip, float fallbackFreq)
    {
        if (clip != null)
        {
            EnsureSource();
            src.PlayOneShot(clip);
        }
        else
        {
            Beep(fallbackFreq, 0.18f);
        }
    }

    static void EnsureSource()
    {
        if (src != null) return;
        var go = new GameObject("SfxSource");
        Object.DontDestroyOnLoad(go);
        src = go.AddComponent<AudioSource>();
        src.spatialBlend = 0f;
    }
}
