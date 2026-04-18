using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Projectile : MonoBehaviour
{
    public float damage = 2f;

    private void OnCollisionEnter(Collision collision)
    {
        BossStateMachine boss = collision.gameObject.GetComponent<BossStateMachine>();

        if (boss != null)
        {
            boss.TakeDamage(damage);
        }

        Destroy(gameObject);
    }
}
