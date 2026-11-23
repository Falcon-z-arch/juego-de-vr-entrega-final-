using System.Collections.Generic;
using UnityEngine;

// Cofre: al tocarlo se ve como se abre la tapa (animacion) y aparece el panel para guardar o sacar objetos.
// Se cierra solo si te alejas. Necesita un Collider normal en el objeto principal.
public class Chest : Interactable
{
    public Transform lid;                     // pivote de la tapa (en la bisagra)
    public float openAngle = 110f;
    public float speed = 6f;
    public float autoCloseDistance = 7f;
    public List<string> contents = new List<string>();

    bool open;
    Quaternion closedRot;
    Quaternion target;
    PartyManager party;

    void Start()
    {
        party = FindFirstObjectByType<PartyManager>();
        closedRot = lid != null ? lid.localRotation : Quaternion.identity;
        target = closedRot;
    }

    public override void Interact(PlayerMovement who)
    {
        if (open)
        {
            if (InventoryUI.I != null) InventoryUI.I.Close();   // esto llama a Close() del cofre
            else Close();
        }
        else
        {
            Open();
        }
    }

    void Open()
    {
        open = true;
        target = closedRot * Quaternion.Euler(openAngle, 0f, 0f);
        Sfx.Ping(sound, 400f);
        HUD.Say("Cofre abierto");
        if (InventoryUI.I != null) InventoryUI.I.Open(this);
    }

    public void Close()
    {
        if (!open) return;
        open = false;
        target = closedRot;
        Sfx.Ping(sound, 300f);
        HUD.Say("Cofre cerrado");
    }

    public void Store(string item)
    {
        contents.Add(item);
        Inventory.Remove(item);      // dispara el refresco del panel
        Sfx.Beep(520f, 0.1f);
        HUD.Say("Guardaste: " + item);
    }

    public void Take(string item)
    {
        if (!contents.Remove(item)) return;
        Inventory.Add(item);         // dispara el refresco del panel
        Sfx.Beep(820f, 0.1f);
        HUD.Say("Sacaste: " + item);
    }

    void Update()
    {
        if (lid != null)
            lid.localRotation = Quaternion.Slerp(lid.localRotation, target, speed * Time.deltaTime);

        if (open && party != null &&
            Vector3.Distance(party.Current.transform.position, transform.position) > autoCloseDistance)
        {
            if (InventoryUI.I != null) InventoryUI.I.Close();
            else Close();
        }
    }
}
