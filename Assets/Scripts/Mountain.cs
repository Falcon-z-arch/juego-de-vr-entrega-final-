using System.Collections;
using UnityEngine;

// Piezas de la montana continua (la crea ComarcaBuilder; RunManager llena cada zona con un puzzle al azar).

// Terraza de una zona. Al entrar el jugador, avisa al RunManager.
public class MountainZone : MonoBehaviour
{
    public int index = 1;              // 1..4
    public string title = "";
    public MountainDoor door;          // puerta del fondo (se abre al resolver el puzzle)
    [HideInInspector] public Transform content;

    void OnTriggerEnter(Collider other)
    {
        if (other.GetComponentInParent<PlayerMovement>() == null) return;
        if (RunManager.I != null) RunManager.I.EnterZone(this);
    }
}

// Puerta que baja al suelo cuando se resuelve el puzzle. Una vez abierta no se vuelve a cerrar.
public class MountainDoor : Activatable
{
    public Renderer rend;
    public float dropHeight = 6.5f;
    public Vector3 checkpointAfter;    // checkpoint al otro lado
    bool open;
    public bool IsOpen => open;
    float baseY = float.NaN, partial, partialTarget;   // avance parcial: la puerta baja un poco con cada encargo

    void Awake() { baseY = transform.position.y; }

    // f = 0..1 de los encargos hechos (la puerta baja hasta ~45 % del recorrido, sigue cerrada)
    public void SetProgress(float f)
    {
        if (open) return;
        partialTarget = Mathf.Clamp01(f) * dropHeight * 0.45f;
    }

    void Update()
    {
        if (open || float.IsNaN(baseY) || Mathf.Approximately(partial, partialTarget)) return;
        partial = Mathf.MoveTowards(partial, partialTarget, 2.5f * Time.deltaTime);
        var p = transform.position; p.y = baseY - partial; transform.position = p;
    }

    public override void Activate()
    {
        if (open) return;
        open = true;
        Sfx.Beep(900f, 0.25f);
        HUD.Say("La puerta se abre. ¡Sube!");
        if (rend != null)
        {
            var m = rend.material;
            m.color = new Color(0.2f, 1f, 0.45f);
            m.EnableKeyword("_EMISSION");
            m.SetColor("_EmissionColor", new Color(0.2f, 1f, 0.45f) * 1.5f);
        }
        Checkpoint.SetPoint(checkpointAfter);
        StartCoroutine(Drop());
    }

    // no se vuelve a cerrar (evita quedar atrapado)
    public override void Deactivate() { }

    IEnumerator Drop()
    {
        Vector3 a = transform.position, b = new Vector3(a.x, (float.IsNaN(baseY) ? a.y : baseY) - dropHeight, a.z);
        float t = 0f;
        while (t < 1f)
        {
            t += Time.deltaTime / 1.3f;
            transform.position = Vector3.Lerp(a, b, Mathf.SmoothStep(0f, 1f, t));
            yield return null;
        }
        var c = GetComponent<Collider>();
        if (c != null) c.enabled = false;
    }
}
