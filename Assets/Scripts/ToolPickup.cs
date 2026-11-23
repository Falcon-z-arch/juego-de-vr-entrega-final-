using UnityEngine;

// Interaccion 11: Tomar una herramienta. Se guarda en el inventario y desaparece de la escena.
public class ToolPickup : Interactable
{
    public string itemName = "Llave inglesa";

    public override void Interact(PlayerMovement who)
    {
        Inventory.Add(itemName);
        Sfx.Ping(sound, 990f);
        HUD.Say("Recogiste: " + itemName);
        SetTargets(true);
        gameObject.SetActive(false);
    }
}
