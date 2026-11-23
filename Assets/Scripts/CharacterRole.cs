using UnityEngine;

public enum Role { Hobbit, Mage, Engineer, Fighter }   // Fighter (Peleador): solo existe en VR

// Interaccion 3 (Hobbit): agacharse. Tambien guarda el rol de cada personaje.
public class CharacterRole : MonoBehaviour
{
    public Role role;
    public float crouchScale = 0.6f;

    bool crouched;
    Vector3 baseScale;

    void Awake() => baseScale = transform.localScale;

    public void ToggleCrouch()
    {
        crouched = !crouched;
        transform.localScale = crouched
            ? new Vector3(baseScale.x, baseScale.y * crouchScale, baseScale.z)
            : baseScale;
        HUD.Say(crouched ? "Te agachas (pasas por huecos bajos)" : "Te levantas");
        Sfx.Beep(crouched ? 300 : 450, 0.1f);
    }
}
