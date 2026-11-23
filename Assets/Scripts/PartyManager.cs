using UnityEngine;

// Elegir que personaje controlar (relevo). Solo el elegido existe en la escena: aparece donde estaba el anterior.
// El artefacto, si se lleva, se ve en la mano (esquina inferior derecha de la vista).
public class PartyManager : MonoBehaviour
{
    public PlayerMovement[] characters;   // hobbit, mago, ingeniero
    public CameraFollow cameraFollow;     // camara en primera persona
    public ArtifactController artifact;

    int current = -1;
    public PlayerMovement Current => characters[Mathf.Max(0, current)];

    void Start()
    {
        if (GetComponent<RunHud>() == null) gameObject.AddComponent<RunHud>();
        if (GetComponent<ArtifactBurden>() == null) gameObject.AddComponent<ArtifactBurden>().party = this;
        Checkpoint.SetStart(characters[0].transform.position);
        for (int k = 1; k < characters.Length; k++) characters[k].gameObject.SetActive(false);
        Select(0, false);
    }

    // Atajos para los botones del panel "Elegir personaje"
    public void SelectHobbit() => SelectIndex(0);
    public void SelectMage() => SelectIndex(1);
    public void SelectEngineer() => SelectIndex(2);

    public int CurrentIndex => current;

    // el Peleador (4o personaje) solo se puede usar con el casco VR puesto
    public bool IsUsable(int i)
    {
        if (i < 0 || i >= characters.Length) return false;
        var r = characters[i].GetComponent<CharacterRole>();
        return r == null || r.role != Role.Fighter || VRRig.Active;
    }

    // salta al siguiente personaje que no este agotado
    public void SwitchNext()
    {
        var burden = GetComponent<ArtifactBurden>();
        for (int k = 1; k < characters.Length; k++)
        {
            int i = (current + k) % characters.Length;
            if (!IsUsable(i)) continue;
            if (burden == null || burden.CanSwitchTo(i)) { Select(i, true); return; }
        }
    }

    public void SelectIndex(int i)
    {
        if (i < 0 || i >= characters.Length || i == current || !IsUsable(i)) return;
        var burden = GetComponent<ArtifactBurden>();
        if (burden != null && !burden.CanSwitchTo(i)) return;
        Select(i, true);
    }

    // relevo forzado (lo usa ArtifactBurden)
    public void ForceSelect(int i)
    {
        if (i < 0 || i >= characters.Length || i == current) return;
        Select(i, true);
    }

    // Conectar al boton "Saltar"
    public void Jump() => Current.Jump();

    void Select(int i, bool feedback)
    {
        var next = characters[i];
        next.gameObject.SetActive(true);

        if (current >= 0)
        {
            var prev = characters[current];
            next.Teleport(prev.transform.position);
            next.transform.rotation = prev.transform.rotation;
            prev.controlled = false;
            prev.gameObject.SetActive(false);
        }

        current = i;
        next.controlled = true;
        if (cameraFollow != null) cameraFollow.target = next.transform;

        bool carries = artifact != null && artifact.IsCarried;
        if (carries)
            artifact.AttachTo(cameraFollow != null ? cameraFollow.transform : next.transform);

        if (feedback)
        {
            Sfx.Beep(760f, 0.15f);
            HUD.Say("Ahora eres: " + next.name);
            var rl = next.GetComponent<CharacterRole>();
            Color fx = rl == null ? Color.white : rl.role == Role.Hobbit ? new Color(0.45f, 0.95f, 0.4f) : rl.role == Role.Mage ? new Color(0.65f, 0.4f, 1f) : rl.role == Role.Engineer ? new Color(1f, 0.6f, 0.15f) : new Color(1f, 0.2f, 0.15f);
            SwitchFx.Play(next.transform.position, fx);
        }
    }
}
