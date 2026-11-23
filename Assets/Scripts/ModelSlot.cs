using UnityEngine;

// Ranura para tus modelos 3D. Si existe el prefab Assets/Resources/Modelos/<slotName>.prefab, reemplaza
// lo que se ve de este objeto (cubo, roca, etc.) por tu modelo, ajustandolo al tamano. Las colisiones no cambian.
// Si no existe, no pasa nada y se queda la forma simple.
public class ModelSlot : MonoBehaviour
{
    public string slotName;
    public float scaleMultiplier = 1f;    // por si el modelo queda chico o grande
    public Vector3 rotationOffset;        // por si el modelo viene girado

    void Awake()
    {
        if (string.IsNullOrEmpty(slotName)) return;
        var prefab = Resources.Load<GameObject>("Modelos/" + slotName);
        if (prefab == null) return;

        var rends = GetComponents<Renderer>();
        if (rends.Length == 0) rends = GetComponentsInChildren<Renderer>();

        // tamano objetivo = el de la forma simple
        Bounds target = new Bounds(transform.position, Vector3.zero);
        bool first = true;
        foreach (var r in rends)
        {
            if (first) { target = r.bounds; first = false; }
            else target.Encapsulate(r.bounds);
        }
        foreach (var r in rends) r.enabled = false;

        var model = Instantiate(prefab, transform);
        model.name = "Modelo_" + slotName;
        foreach (var c in model.GetComponentsInChildren<Collider>()) Destroy(c);
        model.transform.localRotation = Quaternion.Euler(rotationOffset);

        // medir el modelo y escalarlo para que quepa en la forma simple
        var mr = model.GetComponentsInChildren<Renderer>();
        if (mr.Length == 0) return;
        Bounds mb = mr[0].bounds;
        foreach (var r in mr) mb.Encapsulate(r.bounds);

        float tm = Mathf.Max(target.size.x, target.size.y, target.size.z);
        float mm = Mathf.Max(mb.size.x, mb.size.y, mb.size.z);
        if (mm > 0.0001f && tm > 0.0001f)
        {
            float k = tm / mm * scaleMultiplier;
            model.transform.localScale *= k;
        }

        // centrar sobre la forma simple
        mb = mr[0].bounds;
        foreach (var r in mr) mb.Encapsulate(r.bounds);
        model.transform.position += target.center - mb.center;
    }
}
