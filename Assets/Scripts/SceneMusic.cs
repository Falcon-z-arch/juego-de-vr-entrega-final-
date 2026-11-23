using UnityEngine;

// Pone la musica de la escena al empezar. En el volcan, si va a sonar la cinematica, ella se encarga.
public class SceneMusic : MonoBehaviour
{
    public string key = "menu";

    void Start()
    {
        if (key == "volcan" && IntroCinematic.Pending) return;
        MusicPlayer.Play(key);
    }
}
