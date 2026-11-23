using UnityEngine;

// Piezas de la Galeria que se arman al ejecutar (modelos de Resources, jefe, personajes con su diseno, armas).
public class GalleryProp : MonoBehaviour
{
    public enum Mode { Character, Prefab, Boss, Weapons }
    public Mode mode;
    public Role role;
    public string resource;     // Resources/<ruta> sin extension (modo Prefab)
    public float scale = 1f;
    public float spin = 25f;    // grados por segundo (0 = quieto)

    Transform spinner;

    void Start()
    {
        switch (mode)
        {
            case Mode.Character:
            {
                var an = GetComponentInChildren<Animator>();
                if (an != null) CharacterDesign.Apply(transform, an, role);
                break;
            }
            case Mode.Prefab:
            {
                var prefab = Resources.Load<GameObject>(resource);
                if (prefab == null) { Debug.LogWarning("Galeria: falta Resources/" + resource + " (ejecuta Certamen2 > 0 - Construir TODO)"); return; }
                var inst = Instantiate(prefab, transform);
                inst.transform.localPosition = Vector3.zero;
                inst.transform.localRotation = Quaternion.identity;
                inst.transform.localScale = inst.transform.localScale * scale;
                var e = inst.GetComponent<Enemy>(); if (e != null) e.enabled = false;
                foreach (var col in inst.GetComponentsInChildren<Collider>()) col.enabled = false;
                var act = inst.GetComponent<ActorAnim>(); if (act != null) act.manual = false;
                spinner = inst.transform;
                break;
            }
            case Mode.Boss:
            {
                var look = new GameObject("CronarcaVitrina").transform;
                look.SetParent(transform, false);
                look.localScale = Vector3.one * scale;
                CronarcaLook.Build(look);
                spinner = look;
                break;
            }
            case Mode.Weapons:
            {
                WeaponItem.Kind[] ks = { WeaponItem.Kind.Sword, WeaponItem.Kind.Axe, WeaponItem.Kind.Mace, WeaponItem.Kind.Shield };
                for (int i = 0; i < ks.Length; i++)
                    WeaponItem.Make(ks[i], transform.position + new Vector3(0f, 1.2f + i * 0.2f, (i - 1.5f) * 0.7f));
                break;
            }
        }
    }

    void Update()
    {
        if (spinner != null && spin != 0f) spinner.Rotate(0f, spin * Time.deltaTime, 0f, Space.Self);
    }
}
