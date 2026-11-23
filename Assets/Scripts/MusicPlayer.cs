using UnityEngine;

// Musica de fondo. Si existe tu propio archivo en Assets/Resources/Audio/Musica_<nombre> (mp3, wav u ogg) se usa ese.
// Si no, genera una pista ambiental por codigo, asi el juego nunca queda mudo.
// Nombres: Musica_menu, Musica_intro, Musica_volcan, Musica_pruebas, Musica_galeria
public static class MusicPlayer
{
    static AudioSource src;
    static string current;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    static void ResetStatics() { src = null; current = null; }

    public static void Play(string key, float volume = 0.35f)
    {
        var song = Resources.Load<AudioClip>("Audio/Musica");   // tu cancion medieval: suena en todo el juego
        if (song != null && src != null && src.clip == song && src.isPlaying) { src.volume = volume; current = key; return; }
        if (song == null && key == current && src != null && src.isPlaying) return;
        current = key;

        if (src == null)
        {
            var go = new GameObject("MusicSource");
            Object.DontDestroyOnLoad(go);
            src = go.AddComponent<AudioSource>();
            src.loop = true;
            src.spatialBlend = 0f;
        }

        var clip = song != null ? song : Resources.Load<AudioClip>("Audio/Musica_" + key);
        if (clip == null) clip = Generate(key);

        src.clip = clip;
        src.volume = volume;
        src.Play();
    }

    public static void Stop()
    {
        if (src != null) src.Stop();
        current = null;
    }

    // ------------------------------------------------------------------ musica generada
    const int Rate = 22050;
    const float Dur = 16f;   // se repite cada 16 s; las frecuencias se ajustan para que el empalme no suene

    static float Snap(float f) => Mathf.Round(f * Dur) / Dur;

    static AudioClip Generate(string key)
    {
        int n = (int)(Rate * Dur);
        var d = new float[n];

        switch (key)
        {
            case "intro":
                Pad(d, new[] { 55f, 82.4f, 110f }, 0.16f);
                Bells(d, new[] { 440f, 523.3f, 392f, 329.6f }, 4f, 0.12f, 1.5f);
                break;
            case "volcan":
                Pad(d, new[] { 55f, 58.3f, 110f }, 0.15f);
                Pulse(d, 0.5f, 60f, 0.3f);
                Pluck(d, new[] { 220f, 261.6f, 220f, 233.1f, 196f, 220f, 261.6f, 293.7f }, 0.5f, 0.09f);
                break;
            case "boss":
                Pad(d, new[] { 55f, 58.3f, 82.4f }, 0.14f);
                Pulse(d, 0.25f, 55f, 0.28f);
                Pluck(d, new[] { 220f, 233.1f, 220f, 196f, 220f, 261.6f, 246.9f, 220f }, 0.25f, 0.09f);
                Bells(d, new[] { 440f, 466.2f, 392f, 415.3f }, 4f, 0.08f, 2f);
                break;
            case "comarca":
                Pad(d, new[] { 130.8f, 196f, 261.6f }, 0.1f);
                Pluck(d, new[] { 392f, 440f, 523.3f, 587.3f, 523.3f, 440f, 392f, 330f, 392f, 440f, 523.3f, 659.3f, 587.3f, 523.3f, 440f, 392f }, 1f, 0.13f);
                break;
            case "final":
                Pad(d, new[] { 130.8f, 164.8f, 196f, 261.6f }, 0.12f);
                Bells(d, new[] { 523.3f, 659.3f, 784f, 1046.5f }, 2f, 0.13f, 1.2f);
                break;
            case "pruebas":
                Pad(d, new[] { 130.8f, 164.8f, 196f }, 0.11f);
                Pluck(d, new[] { 261.6f, 329.6f, 392f, 523.3f, 392f, 329.6f, 293.7f, 392f }, 1f, 0.14f);
                break;
            default: // menu, galeria
                Pad(d, new[] { 110f, 130.8f, 164.8f }, 0.12f);
                Pluck(d, new[] { 220f, 261.6f, 329.6f, 392f, 329.6f, 261.6f, 392f, 329.6f, 220f, 261.6f, 329.6f, 440f, 392f, 329.6f, 261.6f, 196f }, 1f, 0.13f);
                break;
        }

        for (int i = 0; i < n; i++) d[i] = Mathf.Clamp(d[i], -0.95f, 0.95f);

        var clip = AudioClip.Create("musica_" + key, n, 1, Rate, false);
        clip.SetData(d, 0);
        return clip;
    }

    // acordes sostenidos con vibrato lento (1 ciclo por pista)
    static void Pad(float[] d, float[] freqs, float vol)
    {
        for (int k = 0; k < freqs.Length; k++)
        {
            float f = Snap(freqs[k]);
            float f2 = Snap(freqs[k] * 1.004f);
            for (int i = 0; i < d.Length; i++)
            {
                float t = i / (float)Rate;
                float lfo = 0.65f + 0.35f * Mathf.Sin(2f * Mathf.PI * t / Dur);
                d[i] += (Mathf.Sin(2f * Mathf.PI * f * t) + Mathf.Sin(2f * Mathf.PI * f2 * t)) * 0.5f * vol * lfo;
            }
        }
    }

    // notas pulsadas, una cada "step" segundos
    static void Pluck(float[] d, float[] notes, float step, float vol)
    {
        int stepN = (int)(Rate * step);
        int count = (int)(Dur / step);
        for (int s = 0; s < count; s++)
        {
            float f = Snap(notes[s % notes.Length]);
            int start = s * stepN;
            for (int i = 0; i < stepN && start + i < d.Length; i++)
            {
                float t = i / (float)Rate;
                float env = Mathf.Min(1f, t * 80f) * Mathf.Exp(-4.5f * t / step);
                d[start + i] += Mathf.Sin(2f * Mathf.PI * f * t) * vol * env;
            }
        }
    }

    // golpe grave periodico (latido del volcan)
    static void Pulse(float[] d, float step, float freq, float vol)
    {
        int stepN = (int)(Rate * step);
        int count = (int)(Dur / step);
        for (int s = 0; s < count; s++)
        {
            int start = s * stepN;
            for (int i = 0; i < stepN && start + i < d.Length; i++)
            {
                float t = i / (float)Rate;
                float f = freq * (1f + 1.5f * Mathf.Exp(-30f * t));
                d[start + i] += Mathf.Sin(2f * Mathf.PI * f * t) * vol * Mathf.Exp(-9f * t);
            }
        }
    }

    // campanas espaciadas
    static void Bells(float[] d, float[] notes, float step, float vol, float decay)
    {
        int stepN = (int)(Rate * step);
        int count = (int)(Dur / step);
        for (int s = 0; s < count; s++)
        {
            float f = Snap(notes[s % notes.Length]);
            int start = s * stepN;
            for (int i = 0; i < stepN && start + i < d.Length; i++)
            {
                float t = i / (float)Rate;
                float env = Mathf.Min(1f, t * 60f) * Mathf.Exp(-decay * t);
                d[start + i] += (Mathf.Sin(2f * Mathf.PI * f * t) + 0.4f * Mathf.Sin(2f * Mathf.PI * f * 2.01f * t)) * vol * env;
            }
        }
    }
}
