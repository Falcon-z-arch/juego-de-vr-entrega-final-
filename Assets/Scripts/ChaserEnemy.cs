using UnityEngine;
using UnityEngine.AI;

// Enemigo perseguidor ("la Sombra"): persigue a quien lleva el artefacto y se acelera con el tiempo.
// Opcional para la zona de pruebas. Requiere un NavMesh horneado (AI Navigation > NavMeshSurface).
[RequireComponent(typeof(NavMeshAgent))]
public class ChaserEnemy : MonoBehaviour
{
    public PartyManager party;
    public bool active = true;
    public float baseSpeed = 2.5f;
    public float acceleration = 0.05f;   // aumento de velocidad por segundo
    public float maxSpeed = 5.5f;
    public float catchDistance = 1.2f;

    NavMeshAgent agent;
    Vector3 home;

    void Start()
    {
        agent = GetComponent<NavMeshAgent>();
        agent.speed = baseSpeed;
        home = transform.position;
    }

    void Update()
    {
        if (!active || party == null || !agent.isOnNavMesh) return;

        Transform target = party.Current.transform;
        agent.speed = Mathf.Min(maxSpeed, agent.speed + acceleration * Time.deltaTime);
        agent.SetDestination(target.position);

        if (Vector3.Distance(transform.position, target.position) < catchDistance)
            Caught();
    }

    void Caught()
    {
        HUD.Say("La Sombra te alcanzo. Vuelves al checkpoint");
        Sfx.Beep(120f, 0.5f);
        Checkpoint.Respawn(party.Current);
        agent.Warp(home);
        agent.speed = baseSpeed;
    }
}
