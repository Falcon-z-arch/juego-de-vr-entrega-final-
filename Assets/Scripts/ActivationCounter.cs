using UnityEngine;

// Compuerta "Y": activa sus objetivos solo cuando todas las placas conectadas estan activas.
// Ej.: dos placas con required = 2 -> el respiradero solo se enciende con los dos cubos puestos.
public class ActivationCounter : Activatable
{
    public int required = 2;
    public Activatable[] targets;

    int count;
    bool fired;

    public override void Activate()
    {
        count++;
        Check();
    }

    public override void Deactivate()
    {
        count = Mathf.Max(0, count - 1);
        Check();
    }

    void Check()
    {
        bool ok = count >= required;
        if (ok == fired) return;
        fired = ok;

        if (ok) HUD.Say("Todas las placas estan activas");
        if (targets == null) return;
        foreach (var t in targets)
        {
            if (t == null) continue;
            if (ok) t.Activate(); else t.Deactivate();
        }
    }
}
