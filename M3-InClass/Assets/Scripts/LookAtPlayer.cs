using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;

public class LookAtPlayer : MonoBehaviour
{
    public Transform player; // Drag player object here in inspector
    public float turnSpeed = 10f; // How fast the agent turns
    private NavMeshAgent agent;

    void Start()
    {
        agent = GetComponent<NavMeshAgent>();
    }

    void Update()
    {
        if (player == null) return;

        // Calculate direction to player, ignoring height differences
        Vector3 direction = (player.position - transform.position).normalized;
        direction.y = 0; // Ensures the enemy only rotates around Y axis

        if (direction != Vector3.zero)
        {
            // Calculate target rotation
            Quaternion lookRotation = Quaternion.LookRotation(direction);

            // Smoothly rotate toward target
            transform.rotation = Quaternion.Slerp(transform.rotation, lookRotation, Time.deltaTime * turnSpeed);
        }
    }
}
