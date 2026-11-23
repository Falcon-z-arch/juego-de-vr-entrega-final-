using System.Collections;
using UnityEngine;

// Interaccion 6 (Ingeniero): gancho. Arrastra al personaje hasta "landing".
// Se usa con el boton Habilidad cuando el ingeniero lleva el artefacto y hay un punto cerca.
public class GrapplePoint : MonoBehaviour
{
    public Transform landing;     // donde queda parado el personaje
    public float speed = 14f;

    bool busy;

    public void Pull(PlayerMovement who)
    {
        if (!busy) StartCoroutine(PullRoutine(who));
    }

    IEnumerator PullRoutine(PlayerMovement who)
    {
        busy = true;
        who.locked = true;

        Vector3 target = landing != null ? landing.position : transform.position + Vector3.up * 1.5f;

        // "cuerda" visual: un cubo delgado estirado entre el personaje y el punto
        var rope = GameObject.CreatePrimitive(PrimitiveType.Cube);
        Destroy(rope.GetComponent<Collider>());

        Sfx.Beep(500, 0.12f);
        HUD.Say("Gancho!");

        var cc = who.Controller;
        float t = 0f;
        while (Vector3.Distance(who.transform.position, target) > 0.15f && t < 2.5f)
        {
            Vector3 pos = who.transform.position;
            Vector3 step = Vector3.MoveTowards(pos, target, speed * Time.deltaTime) - pos;
            cc.Move(step);

            Vector3 a = who.transform.position + Vector3.up;
            Vector3 b = transform.position;
            rope.transform.position = (a + b) / 2f;
            rope.transform.LookAt(b);
            rope.transform.localScale = new Vector3(0.05f, 0.05f, Vector3.Distance(a, b));

            t += Time.deltaTime;
            yield return null;
        }

        Destroy(rope);
        who.locked = false;
        busy = false;
    }
}
