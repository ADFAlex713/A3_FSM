using System.Collections;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using UnityEngine.AI;

public class StateMachineSimple : MonoBehaviour
{
    public enum State { Idle, Patrol, Search, Chase }

    [Header("Scene Refs")]
    public GameObject character;
    public Transform[] waypoints;


    [Header("Config Values")]
    public float idleThreshold = 1.0f;
    public float waypointThreshold = 0.5f;
    public float searchThreshold = 5f;
    public float viewRadius = 10f;
    public float viewAngle = 60f;


    float idleTime = 0.0f;   
    State state = State.Idle;
    int patrolIndex = 0;
    float searchTime = 0;

    NavMeshAgent agent;

    bool viewEnabled = false;
    bool canSeePlayer = false;

    private void Awake()
    {
        agent = GetComponent<NavMeshAgent>();

        state = State.Idle;
        idleTime = Time.time;
    }

    private void Update()
    {
        switch (state)
        {
            case State.Idle:
                Idle();
                break;
            case State.Patrol:
                Patrol();
                break;
            case State.Search:
                Search();
                break;
            case State.Chase:
                Chase();
                break;
        }
    }
    void Idle()
    {
        viewEnabled = false;

        float timeElapsed = Time.time - idleTime;
        if (timeElapsed >= idleThreshold)
        {
            EnterPatrol();
        }
    }

    void EnterPatrol()
    {
        state = State.Patrol;
        patrolIndex++;
        if (patrolIndex >= waypoints.Length) patrolIndex = 0;
    }
    void Patrol()
    {
        Vector3 waypoint = waypoints[patrolIndex].position;

        agent.SetDestination(waypoint);

        viewEnabled = true;
        canSeePlayer = InViewCone();

        float distance = Vector3.Distance(transform.position, waypoint);

        if (distance < waypointThreshold)
        {
            patrolIndex++;
            if(patrolIndex >= waypoints.Length) patrolIndex = 0;

            state = State.Idle;
            idleTime = Time.time;
        }

        if(canSeePlayer)
        {
            state = State.Chase;
        }
    }
    void Chase()
    {
        agent.SetDestination(character.transform.position);

        canSeePlayer = InViewCone();
        if (canSeePlayer)
        {
            state = State.Search;
            searchTime = 0f;
        }

    }
    void Search()
    {
        agent.SetDestination(transform.forward + transform.right);

        float elapsedSearchTime = Time.time - searchTime;
        if (elapsedSearchTime > searchThreshold)
        {
            EnterPatrol();
        }
        canSeePlayer = InViewCone();
        if(canSeePlayer)
        {
            state = State.Chase;
        }
    }

    bool InViewCone()
    {
        if (Vector3.Distance(transform.position, character.transform.position) > viewRadius)
            return false;

        Vector3 npcToCharacter = character.transform.position - transform.position;
        if (Vector3.Angle(transform.forward, npcToCharacter) > 0.5f * viewAngle)
            return false;

        Vector3 toCharacterDir = npcToCharacter.normalized;
        if (Physics.Raycast(transform.position, toCharacterDir, out RaycastHit ray, viewRadius))
        {
            return ray.transform == character.transform;
        }

        return false;
    }

    private void OnDrawGizmos()
    {
        Gizmos.color = Color.red;
        foreach (Transform waypoint in waypoints)
        {
            Gizmos.DrawWireSphere(waypoint.position, 0.5f);
        }

        if (viewEnabled)
        {
            Handles.color = new Color(0f, 1f, 1f, 0.25f);

            if (canSeePlayer) Handles.color = new Color(1f, 0f, 0f, 0.25f);

            Handles.DrawSolidArc(transform.position, Vector3.up, transform.forward, viewAngle / 2, viewRadius);
            Handles.DrawSolidArc(transform.position, Vector3.up, transform.forward, -viewAngle / 2, viewRadius);
        }

    }

}
