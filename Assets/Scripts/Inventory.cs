using System.Collections.Generic;
using UnityEngine;

// Inventario simple por nombre de objeto. El artefacto NO esta aqui: lo maneja ArtifactController.
public static class Inventory
{
    static readonly List<string> items = new List<string>();

    public static event System.Action Changed;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    static void ResetStatics()
    {
        items.Clear();
        Changed = null;
    }

    public static IReadOnlyList<string> Items => items;

    public static void Add(string name)
    {
        if (!items.Contains(name)) items.Add(name);
        Changed?.Invoke();
    }

    public static bool Remove(string name)
    {
        bool removed = items.Remove(name);
        if (removed) Changed?.Invoke();
        return removed;
    }

    public static bool Has(string name) => items.Contains(name);

    public static void Clear()
    {
        items.Clear();
        Changed?.Invoke();
    }
}
