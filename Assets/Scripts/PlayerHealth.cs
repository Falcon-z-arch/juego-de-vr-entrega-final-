using UnityEngine;

// Vidas del jugador: cada golpe de roca quita una y empuja con fuerza.
// Al llegar a 0 vuelves al ultimo checkpoint con las vidas llenas.
public class PlayerHealth : MonoBehaviour
{
    public static PlayerHealth I;

    public int maxLives = 4;
    public float invulnerableTime = 1.8f;

    int lives;
    int MaxNow => maxLives + RunState.ExtraLives;
    float immuneUntil;

    void Awake()
    {
        I = this;
        lives = MaxNow;
    }

    void Start() => Refresh();

    // reliquia "Corazon de brasa"
    public void AddLife()
    {
        lives++;
        Refresh();
    }

    public static void Hit(PlayerMovement who, Vector3 knock, string cause = "Roca!")
    {
        if (I != null) I.Take(who, knock, cause);
        else Checkpoint.Respawn(who);
    }

    void Take(PlayerMovement who, Vector3 knock, string cause)
    {
        if (Time.time < immuneUntil) return;
        if (DevMode.God) return;   // modo desarrollador: inmortal
        immuneUntil = Time.time + invulnerableTime;

        lives--;
        who.Launch(new Vector3(knock.x, 4.5f, knock.z));
        Sfx.Beep(200f, 0.2f);

        if (lives <= 0)
        {
            if (RunManager.I != null && RunState.InRun)
            {
                RunManager.I.Fail();
                return;
            }
            HUD.Say("Caiste. Vuelves al checkpoint");
            lives = MaxNow;
            Checkpoint.Respawn(who);
        }
        else
        {
            HUD.Say(cause + " Te quedan " + lives + " vidas");
        }
        Refresh();
    }

    void Refresh()
    {
        HUD.SetLife("Vidas: " + new string('O', Mathf.Max(0, lives)) + new string('-', Mathf.Max(0, MaxNow - lives)));
    }
}
