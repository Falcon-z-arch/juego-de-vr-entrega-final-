using UnityEngine;

// Viento de fondo (Assets/Resources/Audio/Viento) que suena bajito durante todo el juego.
// Al derrotar al jefe: se corta la musica y queda solo el viento (momento de paz).
public static class WindAmbience
{
    static AudioSource src;
    static bool peace, hooked;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    static void ResetStatics() { src = null; peace = false; hooked = false; }

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    static void Boot()
    {
        if (!hooked)
        {
            hooked = true;
            UnityEngine.SceneManagement.SceneManager.sceneLoaded += (s, m) =>
            {
                if (peace) { peace = false; if (src != null) src.volume = 0.06f; }
            };
        }
        if (src != null) return;
        var clip = Resources.Load<AudioClip>("Audio/Viento");
        if (clip == null) return;
        var go = new GameObject("VientoAmbiente");
        Object.DontDestroyOnLoad(go);
        src = go.AddComponent<AudioSource>();
        src.clip = clip;
        src.loop = true;
        src.spatialBlend = 0f;
        src.volume = 0.06f;
        src.Play();
    }

    public static void Peace()
    {
        peace = true;
        MusicPlayer.Stop();
        Boot();
        if (src != null) { src.volume = 0.2f; if (!src.isPlaying) src.Play(); }
    }

    public static bool InPeace => peace;
}
