using System.Collections.Generic;
using UnityEngine;

// Estado del roguelike.
//  - Permanente (se guarda en el telefono/PC): monedas del banco y mejoras compradas en la Comarca.
//  - De la partida (se pierde al caer): monedas ganadas, reliquias elegidas en los altares.
public static class RunState
{
    // ---------- permanente ----------
    public static int Bank
    {
        get => PlayerPrefs.GetInt("bank", 0);
        set { PlayerPrefs.SetInt("bank", Mathf.Max(0, value)); PlayerPrefs.Save(); }
    }

    public static int Wins
    {
        get => PlayerPrefs.GetInt("wins", 0);
        set { PlayerPrefs.SetInt("wins", value); PlayerPrefs.Save(); }
    }

    public static int Level(string id) => PlayerPrefs.GetInt("up_" + id, 0);
    public static void SetLevel(string id, int v) { PlayerPrefs.SetInt("up_" + id, v); PlayerPrefs.Save(); }

    // Mejoras de las tiendas
    public class Upgrade
    {
        public string id, vendor, name, desc;
        public int maxLevel;
        public int[] cost;
    }

    public static readonly Upgrade[] Upgrades =
    {
        new Upgrade { id = "armor", vendor = "Herrero", name = "Armadura", desc = "+1 vida maxima", maxLevel = 3, cost = new[] { 15, 30, 50 } },
        new Upgrade { id = "boost", vendor = "Herrero", name = "Relevo ardiente", desc = "+2 s de impulso al relevar", maxLevel = 3, cost = new[] { 15, 25, 40 } },
        new Upgrade { id = "anti", vendor = "Alquimista", name = "Frasco anticorrupcion", desc = "-15% de carga del artefacto", maxLevel = 3, cost = new[] { 20, 35, 55 } },
        new Upgrade { id = "recover", vendor = "Alquimista", name = "Infusion de descanso", desc = "+25% recuperacion de los que descansan", maxLevel = 3, cost = new[] { 15, 25, 40 } },
        new Upgrade { id = "boots", vendor = "Mercader", name = "Botas ligeras", desc = "+8% de velocidad", maxLevel = 3, cost = new[] { 15, 30, 45 } },
        new Upgrade { id = "hook", vendor = "Mercader", name = "Gancho rapido", desc = "-20% espera del gancho", maxLevel = 3, cost = new[] { 20, 30, 45 } },
    };

    // ---------- de la partida ----------
    public static bool InRun;
    public static int RunCoins;
    public static int Floor;
    public static readonly Dictionary<string, int> Boons = new Dictionary<string, int>();

    public class Boon { public string id, name, desc; }

    public static readonly Boon[] AllBoons =
    {
        new Boon { id = "heart",  name = "Corazon de brasa",   desc = "+1 vida ahora y hasta el final de la partida" },
        new Boon { id = "wind",   name = "Botas del viento",   desc = "+15% de velocidad" },
        new Boon { id = "hands",  name = "Manos de relojero",  desc = "-30% espera del gancho" },
        new Boon { id = "arcane", name = "Pulso arcano",       desc = "+1 de poder a la magia" },
        new Boon { id = "seal",   name = "Sello del Ascua",    desc = "-25% de carga del artefacto" },
        new Boon { id = "spring", name = "Resorte",            desc = "+20% de salto" },
        new Boon { id = "gold",   name = "Bolsa sin fondo",    desc = "Las monedas valen 50% mas" },
    };

    public static int Boon_(string id) => Boons.TryGetValue(id, out int v) ? v : 0;

    // multiplicadores que leen los demas scripts
    public static float SpeedMul => (1f + 0.08f * Level("boots")) * (1f + 0.15f * Boon_("wind"));
    public static float JumpMul => 1f + 0.2f * Boon_("spring");
    public static float BurdenMul => Mathf.Max(0.2f, (1f - 0.15f * Level("anti"))) * Mathf.Pow(0.75f, Boon_("seal"));
    public static float RecoverMul => 1f + 0.25f * Level("recover");
    public static float HookCooldownMul => Mathf.Max(0.2f, 1f - 0.2f * Level("hook")) * Mathf.Pow(0.7f, Boon_("hands"));
    public static float BoostTimeAdd => 2f * Level("boost");
    public static int MagicBonus => Boon_("arcane");
    public static int ExtraLives => Level("armor") + Boon_("heart");
    public static float CoinMul => 1f + 0.5f * Boon_("gold");

    public static void StartRun()
    {
        InRun = true;
        RunCoins = 0;
        Floor = 0;
        Boons.Clear();
    }

    public static void AddCoins(int n)
    {
        RunCoins += Mathf.Max(1, Mathf.RoundToInt(n * CoinMul));
    }

    // guarda lo ganado en el banco al terminar (caer o ganar)
    public static int EndRun(bool won)
    {
        int gained = RunCoins + (won ? 50 : 0);
        Bank += gained;
        if (won) Wins++;
        InRun = false;
        RunCoins = 0;
        Boons.Clear();
        return gained;
    }

    public static void AddBoon(string id)
    {
        Boons[id] = Boon_(id) + 1;
    }

    public static List<Boon> RandomBoons(int count)
    {
        var pool = new List<Boon>(AllBoons);
        var res = new List<Boon>();
        while (res.Count < count && pool.Count > 0)
        {
            int i = Random.Range(0, pool.Count);
            res.Add(pool[i]);
            pool.RemoveAt(i);
        }
        return res;
    }

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    static void ResetStatics()
    {
        InRun = false; RunCoins = 0; Floor = 0; Boons.Clear();
    }
}
