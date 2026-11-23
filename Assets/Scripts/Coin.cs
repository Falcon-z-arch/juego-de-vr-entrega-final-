using UnityEngine;

// Moneda: gira y se recoge al pasar cerca. Tambien las suelta el cofre y las bolas de fuego destruidas con magia.
public class Coin : MonoBehaviour
{
    public int value = 1;
    public float pickRadius = 1.4f;

    PartyManager party;
    float spin;

    public static Coin Spawn(Vector3 pos, int value = 1)
    {
        var go = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
        go.name = "Moneda";
        Destroy(go.GetComponent<Collider>());
        go.transform.position = pos;
        go.transform.localScale = new Vector3(0.45f, 0.04f, 0.45f);
        go.transform.rotation = Quaternion.Euler(90f, 0f, 0f);
        go.GetComponent<Renderer>().material.color = new Color(1f, 0.82f, 0.15f);
        var c = go.AddComponent<Coin>();
        c.value = value;
        go.AddComponent<ModelSlot>().slotName = "Moneda";
        return c;
    }

    void Start() => party = FindFirstObjectByType<PartyManager>();

    void Update()
    {
        spin += 180f * Time.deltaTime;
        transform.rotation = Quaternion.Euler(90f, spin, 0f);
        if (party == null) return;

        Vector3 p = party.Current.transform.position;
        if ((p - transform.position).sqrMagnitude < pickRadius * pickRadius)
        {
            RunState.AddCoins(value);
            Sfx.Beep(1200f, 0.06f, 0.35f);
            Destroy(gameObject);
        }
    }
}

// Cofre de monedas: se toca y suelta monedas.
public class CoinChest : Interactable
{
    public int coins = 8;
    bool opened;

    void Awake() => prompt = "Cofre";

    public override void Interact(PlayerMovement who)
    {
        if (opened) return;
        Tutorial.Show("cofre", "Cofre", "Los cofres sueltan monedas. Recogelas caminando cerca.\nLas monedas de la subida se suman a tu banco para gastarlas en la aldea.", Open);
    }

    void Open()
    {
        if (opened) return;
        opened = true;
        Sfx.Beep(700f, 0.15f);
        HUD.Say("Cofre abierto");
        for (int i = 0; i < coins; i++)
        {
            Vector2 o = Random.insideUnitCircle * 1.6f;
            Coin.Spawn(transform.position + new Vector3(o.x, 1.2f, o.y), 1);
        }
        var r = GetComponentInChildren<Renderer>();
        if (r != null) r.material.color = new Color(0.3f, 0.2f, 0.1f);
    }
}
