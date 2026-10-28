using UnityEngine;
using UnityEngine.AI;

public class EnemyHealth : MonoBehaviour
{
    [Header("Health")]
    public float health = 100f;

    [Header("Death")]
    public float destroyDelay = 4f;

    private bool isDead = false;

    private Animator animator;
    private EnemyAI enemyAI;
    private NavMeshAgent agent;

    void Start()
    {
        animator = GetComponentInChildren<Animator>();
        enemyAI = GetComponent<EnemyAI>();
        agent = GetComponent<NavMeshAgent>();
    }

    public void TakeDamage(float damage)
    {
        if (isDead)
            return;

        health -= damage;

        Debug.Log(
            gameObject.name +
            " Health: " +
            health
        );

        if (health <= 0f)
        {
            Die();
        }
    }

    void Die()
    {
        if (isDead)
            return;

        isDead = true;

        Debug.Log(
            gameObject.name +
            " eliminated."
        );

        // Stop the enemy AI
        if (enemyAI != null)
        {
            enemyAI.enabled = false;
        }

        // Stop NavMesh movement
        if (agent != null)
        {
            if (agent.isOnNavMesh)
            {
                agent.isStopped = true;
            }

            agent.enabled = false;
        }

        // Stop walking/running states
        if (animator != null)
        {
            animator.SetBool(
                "IsWalking",
                false
            );

            animator.SetBool(
                "IsRunning",
                false
            );

            // Play death animation
            animator.SetTrigger("Die");
        }

        // Disable enemy colliders
        Collider[] colliders =
            GetComponentsInChildren<Collider>();

        foreach (Collider col in colliders)
        {
            col.enabled = false;
        }

        // Remove enemy after death animation
        Destroy(
            gameObject,
            destroyDelay
        );
    }
}