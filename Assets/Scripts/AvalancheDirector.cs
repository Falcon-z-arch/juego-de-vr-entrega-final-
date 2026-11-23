using UnityEngine;

// Director de la avalancha (estilo Halo): alterna calma -> aviso -> oleada de rocas.
public class AvalancheDirector : MonoBehaviour
{
    public FallingRockSpawner spawner;
    public float calmTime = 12f;
    public float warningTime = 3f;
    public float burstTime = 8f;
    public float calmIntensity = 0.35f;
    public float burstIntensity = 1.5f;

    enum Phase { Calm, Warning, Burst }
    Phase phase = Phase.Calm;
    float timer;

    void Start()
    {
        if (spawner == null) spawner = FindFirstObjectByType<FallingRockSpawner>();
        timer = calmTime;
        Apply();
    }

    void Update()
    {
        timer -= Time.deltaTime;
        if (timer > 0f) return;

        switch (phase)
        {
            case Phase.Calm:
                phase = Phase.Warning; timer = warningTime;
                HUD.Say("¡AVALANCHA! Busca refugio");
                Sfx.Beep(220f, 0.5f);
                break;
            case Phase.Warning:
                phase = Phase.Burst; timer = burstTime;
                break;
            default:
                phase = Phase.Calm; timer = calmTime;
                HUD.Say("La avalancha cesa. ¡Sube!");
                break;
        }
        Apply();
    }

    void Apply()
    {
        if (spawner == null) return;
        spawner.intensity = phase == Phase.Burst ? burstIntensity
                          : phase == Phase.Warning ? calmIntensity
                          : calmIntensity;
    }
}
