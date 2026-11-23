using UnityEngine;

// Sonido de bola de fuego (Assets/Resources/Audio/BolaFuego). Si no existe, cae al pitido de siempre.
public static class FireSfx
{
    static AudioClip clip;
    static float lastTime;

    public static void Play(Vector3 pos, float volume = 0.8f)
    {
        if (clip == null) clip = Resources.Load<AudioClip>("Audio/BolaFuego");
        if (clip == null) { Sfx.Beep(260f, 0.12f); return; }
        if (Time.unscaledTime - lastTime < 0.06f) return;   // evita apilar muchos a la vez
        lastTime = Time.unscaledTime;
        AudioSource.PlayClipAtPoint(clip, pos, volume);
    }
}
