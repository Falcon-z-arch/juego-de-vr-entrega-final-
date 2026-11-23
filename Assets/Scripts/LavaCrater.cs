using UnityEngine;

public class LavaCrater : Interactable
{
    public ArtifactController artifact;
    Hatch hatch;

    void Start()
    {
        // el disco de lava se reemplaza por una escotilla (se sigue tocando el mismo objeto)
        var r = GetComponent<Renderer>();
        if (r != null) r.enabled = false;
        hatch = Hatch.Build(transform);
    }

    public override void Interact(PlayerMovement who)
    {
        if (artifact == null) return;

        if (RunManager.I != null && RunState.InRun && !RunManager.I.BossDead)
        {
            HUD.Say("El Cronarca aun protege la escotilla");
            Sfx.Beep(200f, 0.2f);
            return;
        }

        if (!artifact.IsCarried)
        {
            HUD.Say("Debes llevar el artefacto para arrojarlo");
            Sfx.Beep(200f, 0.2f);
            return;
        }

        if (hatch != null) hatch.Open();
        artifact.ThrowInto(transform.position + Vector3.up * 0.3f);
        SetTargets(true);
    }
}
