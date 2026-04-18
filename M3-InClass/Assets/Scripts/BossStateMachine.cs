using System.Collections;
using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEditor;
using UnityEngine;
using UnityEngine.AI;
using UnityEngine.InputSystem.LowLevel;
using UnityEngine.InputSystem.XR.Haptics;

public class BossStateMachine : MonoBehaviour
{
    // Boss States and Variables
    public enum BossStates { Phase1, Phase2, Stunned, Recover, Dead };

    BossStates state = BossStates.Phase1;
    BossStates previousPhase;

    // Scene Refs
    public GameObject character;
    public Transform[] waypoints;

    // Projectile Variables
    public GameObject smallProjectile;
    public GameObject bigProjectile;
    private GameObject projectilePrefab;
    public Transform firePoint;
    public float minInterval = 1f;
    public float maxInterval = 5f;
    public float projectileSpeed = 20f;
    private bool isShooting = false;

    // Health Variables
    public float maxHealth = 80f;
    private float currentHealth;

    // Stun Variables
    public float stunDuration = 3f;
    public float stunTimer;

    // Navmesh Agent and Movement
    NavMeshAgent agent;
    public float waypointThreshold = 1.1f;
    int waypointIndex;

    // Agent Watching Player
    public float viewRadius = 10f;
    public float viewAngle = 60f;
    bool viewEnabled = false;
    bool canSeePlayer = false;

    private void Awake()
    {
        agent = GetComponent<NavMeshAgent>();
        currentHealth = maxHealth;
        state = BossStates.Phase1;
    }

    // Boss Finite State Machine
    private void Update()
    {
        switch (state)
        {
            case BossStates.Phase1:
                previousPhase = BossStates.Phase1;
                Phase1();
                break;

            case BossStates.Phase2:
                previousPhase = BossStates.Phase2;
                Phase2();
                break;

            case BossStates.Stunned:
                Stunned();
                break;

            case BossStates.Recover:
                Recover();
                break;
            case BossStates.Dead:
                break;
        }
    }
    
    void Phase1()
    {
        viewEnabled = true;
        projectilePrefab = smallProjectile;

        // Navmesh Movement
        Vector3 waypoint = waypoints[waypointIndex].position;

        agent.SetDestination(waypoint);
        agent.speed = 5f;
        agent.acceleration = 20f;

        float distance = Vector3.Distance(transform.position, waypoint);

        if (distance < waypointThreshold)
        {
            waypointIndex++;
            if (waypointIndex >= waypoints.Length) waypointIndex = 0;
        }

        // If the player is within view and withing range then the NPC shoots at the player
        canSeePlayer = InViewCone();
        if (canSeePlayer && !isShooting)
        {
            StartCoroutine(ShootRandomly());
        }

        // Once the NPCs health gets under 40, change the state to phase 2
        if (currentHealth <= maxHealth / 2)
        {
            state = BossStates.Phase2;
        }
    }

    void Phase2()
    {
        viewEnabled = true;
        projectilePrefab = bigProjectile;

        // Navmesh Movement
        Vector3 waypoint = waypoints[waypointIndex].position;

        agent.SetDestination(waypoint);
        agent.speed = 7f;
        agent.acceleration = 30f;

        float distance = Vector3.Distance(transform.position, waypoint);

        if (distance < waypointThreshold)
        {
            waypointIndex++;
            if (waypointIndex >= waypoints.Length) waypointIndex = 0;
        }

        // If the player is within view and withing range then the NPC shoots at the player
        canSeePlayer = InViewCone();
        if (canSeePlayer && !isShooting)
        {
            StartCoroutine(ShootRandomly());
        }

        // If the NPCs health is 0, change the state to dead
        if (currentHealth <= 0)
        {
            state = BossStates.Dead;
        }
    }
    
    // Collision check for player projectiles
    private void OnCollisionEnter(Collision collision)
    {
        if (collision.gameObject.CompareTag("Projectile"))
        {
            TakeDamage(2f);
            StunBoss();      
        }
    }

    public void TakeDamage(float damage)
    {
        currentHealth -= damage;

        if (currentHealth <= maxHealth / 2 && state == BossStates.Phase1)
        {
            state = BossStates.Phase2;
        }

        if (currentHealth <= 0)
        {
            state = BossStates.Dead;
        }
    }

    void Stunned()
    {
        // Stun so boss cant move or attack
        stunTimer -= Time.deltaTime;

        if (stunTimer <= 0)
        {
            state = BossStates.Recover;
        }
    }

    void Recover()
    {
        // recovers to the previous phase that the FSM was in before going into stun
        agent.isStopped = false;
        state = previousPhase;
    }

    public void StunBoss()
    {
        if (state == BossStates.Stunned)
        {
            return;
        }
        stunTimer = stunDuration;
        state = BossStates.Stunned;
        agent.isStopped = true; // lock movement
    }

    IEnumerator ShootRandomly()
    {
        isShooting = true;

        while (canSeePlayer)
        {
            // Wait for a random amount of time
            float waitTime = Random.Range(minInterval, maxInterval);
            yield return new WaitForSeconds(waitTime);
            Shoot();
        }
        isShooting = false;
    }

    void Shoot()
    {
        // Instantiate and fire the projectile
        GameObject projectile = Instantiate(projectilePrefab, firePoint.position, firePoint.rotation);

        Rigidbody rb = projectile.GetComponent<Rigidbody>();

        // Ensure the projectile moves towards the player
        Vector3 dir = (character.transform.position - firePoint.position).normalized;
        rb.velocity = dir * projectileSpeed;
        Destroy(projectile, 5f);
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
