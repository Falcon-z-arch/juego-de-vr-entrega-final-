using System.Collections;
using UnityEngine;

// Interaccion 12: Maquina. Solo arranca si tienes la herramienta requerida.
// Al arrancar: engranaje girando (animacion), luz, humo, sonido y mensaje. Luego activa sus targets.
public class Machine : Interactable
{
    public string requiredItem = "Llave inglesa";
    public Transform gear;
    public Light lamp;
    public ParticleSystem smoke;     // opcional
    public float delay = 1.5f;

    bool running;

    void Start()
    {
        if (lamp != null) lamp.enabled = false;
    }

    public override void Interact(PlayerMovement who)
    {
        if (running) return;

        if (!string.IsNullOrEmpty(requiredItem) && !Inventory.Has(requiredItem))
        {
            HUD.Say("La maquina no arranca. Necesitas: " + requiredItem);
            Sfx.Beep(200f, 0.3f);
            return;
        }
        StartCoroutine(StartUp());
    }

    IEnumerator StartUp()
    {
        running = true;
        HUD.Say("Reparando y encendiendo la maquina...");
        Sfx.Ping(sound, 520f);
        if (lamp != null) lamp.enabled = true;
        if (smoke != null) smoke.Play();

        yield return new WaitForSeconds(delay);

        Sfx.Ping(sound, 880f);
        HUD.Say("Energia conectada");
        SetTargets(true);
    }

    void Update()
    {
        if (running && gear != null) gear.Rotate(0f, 0f, 180f * Time.deltaTime);
    }
}
