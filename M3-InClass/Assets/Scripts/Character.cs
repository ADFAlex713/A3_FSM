using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;
using static BossStateMachine;

public class Character : MonoBehaviour
{
    public GameObject projectilePrefab;
    public Transform firePoint;
    public float projectileSpeed = 20f;

    public float moveSpeed = 5.0f;
    public float gravity = -9.81f;

    CharacterController controller;
    PlayerInput playerInput;
    Vector3 velocity;

    public float slowTimer = 5;

    private bool isSlowed = false;

    void Awake()
    {
        controller = GetComponent<CharacterController>();
        playerInput = GetComponent<PlayerInput>();
    }

    public void Update()
    {
        PlayerMotion();

        // Shoot projectile when "space" is pressed
        if (playerInput.currentActionMap["Fire"].WasPressedThisFrame())
        {
            ShootProjectile();
        }

        if (isSlowed)
        {
            slowTimer -= Time.deltaTime;

            if (slowTimer <= 0)
            {
                moveSpeed = 5f;
                isSlowed = false;
            }
        }
    }
    void PlayerMotion()
    {
        if (controller.isGrounded && velocity.y < 0)
        {
            velocity.y = -2f;
        }
        Vector2 moveDirection = playerInput.currentActionMap["Move"].ReadValue<Vector2>();
        Vector3 move = Vector3.right * moveDirection.x + Vector3.forward * moveDirection.y;
        Vector3 moveVelocity = move * moveSpeed;

        velocity.y += gravity * Time.deltaTime;

        moveVelocity.y = velocity.y;

        controller.Move(moveVelocity * Time.deltaTime);


        Vector3 horizontalVelocity = new Vector3(moveVelocity.x, 0f, moveVelocity.z);
        if (horizontalVelocity.sqrMagnitude > 0.001f)
        {
            Quaternion targetRotation = Quaternion.LookRotation(horizontalVelocity);
            transform.rotation = Quaternion.Slerp(transform.rotation, targetRotation, 15f * Time.deltaTime
            );
        }
    }

    void ShootProjectile()
    {
        // Create projectile at the firePoints position
        GameObject projectile = Instantiate(projectilePrefab, firePoint.position, firePoint.rotation);

        // Shoot projectile by applying force to the rigidbody
        Rigidbody rb = projectile.GetComponent<Rigidbody>();

        Vector3 direction = firePoint.forward;
        rb.velocity = direction * projectileSpeed;
        Destroy(projectile, 3f);
    }

    private void OnCollisionEnter(Collision collision)
    {
        if (collision.gameObject.CompareTag("Projectile"))
        {
            isSlowed = true;
            slowTimer = 5f;
            moveSpeed = 1f;
        }
    }
}
