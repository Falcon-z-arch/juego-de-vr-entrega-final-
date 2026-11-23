using UnityEngine;

// Anima el modelo de un personaje jugable segun lo que se mueve (reposo / caminar / correr / aire).
public class PjAnim : MonoBehaviour
{
    ActorAnim anim;
    CharacterController cc;

    void Start()
    {
        anim = GetComponent<ActorAnim>();
        cc = GetComponentInParent<CharacterController>();
        if (anim != null) anim.manual = true;
        var role = GetComponentInParent<CharacterRole>();
        var an = GetComponentInChildren<Animator>();
        if (role != null && an != null) CharacterDesign.Apply(role.transform, an, role.role);
    }

    void Update()
    {
        if (anim == null || cc == null) return;
        Vector3 v = cc.velocity; v.y = 0f;
        float s = v.magnitude;
        if (!cc.isGrounded && cc.velocity.y > 1f) anim.Play("Jump_Loop", 0.1f);
        else if (!cc.isGrounded && cc.velocity.y < -4f) anim.Play("Jump_Loop", 0.2f);
        else if (s > 4f) anim.Play("Jog_Fwd_Loop", 0.15f);
        else if (s > 0.4f) anim.Play("Walk_Loop", 0.15f);
        else anim.Play("Idle_Loop", 0.2f);
    }
}
