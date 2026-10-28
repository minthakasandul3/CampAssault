using UnityEngine;
using UnityEngine.AI;

[RequireComponent(typeof(NavMeshAgent))]
public class EnemyAI : MonoBehaviour
{
    [Header("References")]
    public Transform player;
    public Transform[] patrolPoints;
    public Animator animator;

    [Header("Detection")]
    public float detectionRange = 15f;
    public float stopChasingRange = 40f;

    [Header("Alert")]
    public float alertMemoryDuration = 12f;

    [Header("Movement Speeds")]
    public float patrolSpeed = 1.8f;
    public float chaseSpeed = 3.5f;

    [Header("Patrol")]
    public float patrolWaitTime = 2f;

    [Header("Attack")]
    public float attackRange = 8f;
    public float attackDamage = 20f;
    public float attackCooldown = 1.5f;

    private NavMeshAgent agent;

    private int currentPatrolPoint = 0;
    private float waitTimer = 0f;
    private float attackTimer = 0f;
    private float alertTimer = 0f;

    private bool chasingPlayer = false;

    private PlayerHealth playerHealth;

    void Start()
    {
        agent = GetComponent<NavMeshAgent>();

        if (animator == null)
        {
            animator = GetComponentInChildren<Animator>();
        }

        if (player != null)
        {
            playerHealth = player.GetComponent<PlayerHealth>();
        }

        if (AgentReady())
        {
            agent.speed = patrolSpeed;
        }

        SetMovementAnimation(false, false);

        if (patrolPoints != null &&
            patrolPoints.Length > 0)
        {
            GoToCurrentPatrolPoint();
        }
    }

    void Update()
    {
        if (player == null)
            return;

        float distanceToPlayer =
            Vector3.Distance(
                transform.position,
                player.position
            );

        attackTimer -= Time.deltaTime;

        if (alertTimer > 0f)
        {
            alertTimer -= Time.deltaTime;
        }

        // Normal visual detection
        if (distanceToPlayer <= detectionRange)
        {
            chasingPlayer = true;
        }

        // Only stop chasing when the enemy is no longer
        // remembering a gunshot / direct hit.
        if (chasingPlayer &&
            distanceToPlayer > stopChasingRange &&
            alertTimer <= 0f)
        {
            StopChasing();
        }

        if (chasingPlayer)
        {
            if (distanceToPlayer <= attackRange)
            {
                AttackPlayer();
            }
            else
            {
                ChasePlayer();
            }
        }
        else
        {
            Patrol();
        }
    }

    void ChasePlayer()
    {
        if (!AgentReady())
        {
            SetMovementAnimation(false, false);
            return;
        }

        agent.isStopped = false;
        agent.speed = chaseSpeed;

        SetMovementAnimation(false, true);

        agent.SetDestination(player.position);
    }

    void AttackPlayer()
    {
        if (AgentReady())
        {
            agent.isStopped = true;
        }

        SetMovementAnimation(false, false);

        FacePlayer();

        if (attackTimer <= 0f)
        {
            if (animator != null)
            {
                animator.SetTrigger("Shoot");
            }

            if (playerHealth != null)
            {
                playerHealth.TakeDamage(
                    attackDamage
                );
            }

            attackTimer = attackCooldown;
        }
    }

    void FacePlayer()
    {
        if (player == null)
            return;

        Vector3 direction =
            player.position - transform.position;

        direction.y = 0f;

        if (direction.sqrMagnitude > 0.001f)
        {
            Quaternion targetRotation =
                Quaternion.LookRotation(direction);

            transform.rotation =
                Quaternion.Slerp(
                    transform.rotation,
                    targetRotation,
                    6f * Time.deltaTime
                );
        }
    }

    void Patrol()
    {
        if (patrolPoints == null ||
            patrolPoints.Length == 0)
        {
            SetMovementAnimation(false, false);
            return;
        }

        if (!AgentReady())
        {
            SetMovementAnimation(false, false);
            return;
        }

        agent.isStopped = false;
        agent.speed = patrolSpeed;

        if (!agent.pathPending &&
            agent.remainingDistance <=
            agent.stoppingDistance)
        {
            SetMovementAnimation(false, false);

            waitTimer += Time.deltaTime;

            if (waitTimer >= patrolWaitTime)
            {
                currentPatrolPoint++;

                if (currentPatrolPoint >=
                    patrolPoints.Length)
                {
                    currentPatrolPoint = 0;
                }

                GoToCurrentPatrolPoint();

                waitTimer = 0f;
            }
        }
        else
        {
            SetMovementAnimation(true, false);
        }
    }

    void GoToCurrentPatrolPoint()
    {
        if (patrolPoints == null ||
            patrolPoints.Length == 0)
        {
            return;
        }

        if (!AgentReady())
            return;

        agent.isStopped = false;
        agent.speed = patrolSpeed;

        agent.SetDestination(
            patrolPoints[
                currentPatrolPoint
            ].position
        );
    }

    void StopChasing()
    {
        chasingPlayer = false;
        alertTimer = 0f;

        if (AgentReady())
        {
            agent.isStopped = false;
            agent.speed = patrolSpeed;
        }

        if (patrolPoints != null &&
            patrolPoints.Length > 0)
        {
            GoToCurrentPatrolPoint();
        }
    }

    void SetMovementAnimation(
        bool walking,
        bool running
    )
    {
        if (animator == null)
            return;

        animator.SetBool(
            "IsWalking",
            walking
        );

        animator.SetBool(
            "IsRunning",
            running
        );
    }

    bool AgentReady()
    {
        return agent != null &&
               agent.enabled &&
               agent.isOnNavMesh;
    }

    // Called when enemy hears the player's gun
    // or is directly shot.
    public void AlertPlayer()
    {
        chasingPlayer = true;

        alertTimer = alertMemoryDuration;
        waitTimer = 0f;

        if (AgentReady())
        {
            agent.isStopped = false;
            agent.speed = chaseSpeed;

            if (player != null)
            {
                agent.SetDestination(
                    player.position
                );
            }
        }

        SetMovementAnimation(false, true);
    }
}