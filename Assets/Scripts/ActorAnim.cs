using UnityEngine;

// Personaje animado con la Universal Animation Library (Animator + controlador creado por el editor).
// - Reposo: idleClip. Con 'route' (2+ puntos del mundo) camina de punto en punto y espera un rato.
// - TalkFor(s): mira al jugador y reproduce la animacion de hablar (lo usa Vendor al conversar).
public class ActorAnim : MonoBehaviour
{
    public string idleClip = "Idle_Loop";
    public string walkClip = "Walk_Loop";
    public string talkClip = "Idle_Talking_Loop";
    public Vector3[] route;
    public float speed = 1.2f;
    public float pauseTime = 4f;
    public bool manual;      // true: otro script (Enemy) decide las animaciones

    Animator an;
    string cur;
    int target = 1;
    float wait;
    float talkUntil;

    void Awake()
    {
        an = GetComponentInChildren<Animator>();
        if (an != null) an.applyRootMotion = false;
    }

    void Start() => Play(idleClip, 0f);

    public void Play(string clip, float fade = 0.2f)
    {
        if (an == null || an.runtimeAnimatorController == null || clip == cur) return;
        if (!an.HasState(0, Animator.StringToHash(clip))) return;
        cur = clip;
        an.CrossFadeInFixedTime(clip, fade);
    }

    public void TalkFor(float seconds) => talkUntil = Time.time + seconds;

    void Face(Vector3 dir)
    {
        dir.y = 0f;
        if (dir.sqrMagnitude < 0.0001f) return;
        transform.rotation = Quaternion.Slerp(transform.rotation, Quaternion.LookRotation(dir), 6f * Time.deltaTime);
    }

    // reinicia el clip aunque ya sea el actual (ataques)
    public void Force(string clip, float fade = 0.1f)
    {
        if (an == null || an.runtimeAnimatorController == null) return;
        if (!an.HasState(0, Animator.StringToHash(clip))) return;
        cur = clip;
        an.CrossFadeInFixedTime(clip, fade, 0, 0f);
    }

    void Update()
    {
        if (manual) return;
        if (Time.time < talkUntil)
        {
            Play(talkClip);
            var cam = Camera.main;
            if (cam != null) Face(cam.transform.position - transform.position);
            return;
        }
        if (route == null || route.Length < 2) { Play(idleClip); return; }
        if (wait > 0f) { wait -= Time.deltaTime; Play(idleClip); return; }
        Vector3 d = route[target] - transform.position;
        d.y = 0f;
        if (d.magnitude < 0.15f) { target = (target + 1) % route.Length; wait = pauseTime; return; }
        Play(walkClip);
        Face(d);
        transform.position += d.normalized * speed * Time.deltaTime;
    }
}
