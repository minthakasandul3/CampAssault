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
    public float sightRange = 70f;
    public float fieldOfView = 110f;
    public float stopChasingRange = 90f;

    [Header("Alert")]
    public float alertMemoryDuration = 12f;

    [Header("Movement Speeds")]
    public float patrolSpeed = 1.8f;
    public float chaseSpeed = 3.5f;

    [Header("Patrol")]
    public float patrolWaitTime = 2f;
    public float patrolTurnSpeed = 5f;

    [Header("Attack")]
    public float attackRange = 15f;
    public float attackDamage = 10f;

    [Header("Burst Fire")]
    public int shotsPerBurst = 3;
    public float timeBetweenShots = 0.25f;
    public float timeBetweenBursts = 1.5f;

    private NavMeshAgent agent;
    private PlayerHealth playerHealth;

    private int currentPatrolPoint = 0;

    private float waitTimer = 0f;
    private float alertTimer = 0f;

    private bool chasingPlayer = false;
    private bool playerDeathHandled = false;

    private bool isBursting = false;
    private int shotsFiredInBurst = 0;
    private float shotTimer = 0f;
    private float burstTimer = 0f;

    void Start()
    {
        agent = GetComponent<NavMeshAgent>();

        if (animator == null)
        {
            animator = GetComponentInChildren<Animator>();
        }

        if (player != null)
        {
            playerHealth =
                player.GetComponent<PlayerHealth>();
        }

        if (AgentReady())
        {
            agent.speed = patrolSpeed;
        }

        SetMovementAnimation(false, false);
        SetCombatAnimation(false);

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

        if (playerHealth != null &&
            playerHealth.IsDead)
        {
            if (!playerDeathHandled)
            {
                HandlePlayerDeath();
                playerDeathHandled = true;
            }

            return;
        }

        if (alertTimer > 0f)
        {
            alertTimer -= Time.deltaTime;
        }

        if (burstTimer > 0f)
        {
            burstTimer -= Time.deltaTime;
        }

        if (shotTimer > 0f)
        {
            shotTimer -= Time.deltaTime;
        }

        float distanceToPlayer =
            Vector3.Distance(
                transform.position,
                player.position
            );

        bool canSeePlayer =
            CanSeePlayer();

        if (distanceToPlayer <= detectionRange ||
            canSeePlayer)
        {
            chasingPlayer = true;
            alertTimer = alertMemoryDuration;
        }

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

    bool CanSeePlayer()
    {
        if (player == null)
            return false;

        Vector3 enemyEyePosition =
            transform.position +
            Vector3.up * 1.5f;

        Vector3 playerTargetPosition =
            player.position;

        CharacterController playerController =
            player.GetComponent<CharacterController>();

        if (playerController != null)
        {
            playerTargetPosition =
                playerController.bounds.center;
        }
        else
        {
            Collider playerCollider =
                player.GetComponentInChildren<Collider>();

            if (playerCollider != null)
            {
                playerTargetPosition =
                    playerCollider.bounds.center;
            }
        }

        Vector3 directionToPlayer =
            playerTargetPosition -
            enemyEyePosition;

        float distanceToPlayer =
            directionToPlayer.magnitude;

        if (distanceToPlayer > sightRange)
            return false;

        directionToPlayer.Normalize();

        float angleToPlayer =
            Vector3.Angle(
                transform.forward,
                directionToPlayer
            );

        if (angleToPlayer >
            fieldOfView * 0.5f)
        {
            return false;
        }

        RaycastHit[] hits =
            Physics.RaycastAll(
                enemyEyePosition,
                directionToPlayer,
                distanceToPlayer,
                Physics.AllLayers,
                QueryTriggerInteraction.Ignore
            );

        System.Array.Sort(
            hits,
            (a, b) =>
                a.distance.CompareTo(
                    b.distance
                )
        );

        foreach (RaycastHit hit in hits)
        {
            // Ignore this enemy
            if (hit.transform == transform ||
                hit.transform.IsChildOf(transform))
            {
                continue;
            }

            // Player visible
            if (hit.transform == player ||
                hit.transform.IsChildOf(player))
            {
                Debug.DrawLine(
                    enemyEyePosition,
                    playerTargetPosition,
                    Color.green
                );

                return true;
            }

            // Something blocks vision
            Debug.DrawLine(
                enemyEyePosition,
                hit.point,
                Color.red
            );

            return false;
        }

        return false;
    }

    void ChasePlayer()
    {
        ResetBurst();

        if (!AgentReady())
        {
            SetMovementAnimation(
                false,
                false
            );

            return;
        }

        agent.isStopped = false;
        agent.speed = chaseSpeed;

        SetCombatAnimation(true);

        SetMovementAnimation(
            false,
            true
        );

        agent.SetDestination(
            player.position
        );
    }

    void AttackPlayer()
    {
        if (AgentReady())
        {
            agent.isStopped = true;
        }

        SetCombatAnimation(true);

        SetMovementAnimation(
            false,
            false
        );

        FacePlayer();

        if (isBursting)
        {
            if (shotTimer <= 0f)
            {
                FireShot();
            }

            return;
        }

        if (burstTimer <= 0f)
        {
            StartBurst();
        }
    }

    void StartBurst()
    {
        isBursting = true;
        shotsFiredInBurst = 0;
        shotTimer = 0f;

        FireShot();
    }

    void FireShot()
    {
        if (playerHealth == null ||
            playerHealth.IsDead)
        {
            ResetBurst();
            return;
        }

        if (animator != null)
        {
            animator.SetTrigger(
                "Shoot"
            );
        }

        playerHealth.TakeDamage(
            attackDamage
        );

        shotsFiredInBurst++;

        if (shotsFiredInBurst >=
            shotsPerBurst)
        {
            isBursting = false;
            shotsFiredInBurst = 0;

            burstTimer =
                timeBetweenBursts;

            shotTimer = 0f;
        }
        else
        {
            shotTimer =
                timeBetweenShots;
        }
    }

    void ResetBurst()
    {
        isBursting = false;
        shotsFiredInBurst = 0;
        shotTimer = 0f;
    }

    void FacePlayer()
    {
        if (player == null)
            return;

        Vector3 direction =
            player.position -
            transform.position;

        direction.y = 0f;

        if (direction.sqrMagnitude >
            0.001f)
        {
            Quaternion targetRotation =
                Quaternion.LookRotation(
                    direction
                );

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
        ResetBurst();

        SetCombatAnimation(false);

        if (patrolPoints == null ||
            patrolPoints.Length == 0)
        {
            SetMovementAnimation(
                false,
                false
            );

            return;
        }

        if (!AgentReady())
        {
            SetMovementAnimation(
                false,
                false
            );

            return;
        }

        agent.isStopped = false;
        agent.speed = patrolSpeed;

        if (!agent.pathPending &&
            agent.remainingDistance <=
            agent.stoppingDistance)
        {
            // Enemy reached the patrol point
            SetMovementAnimation(
                false,
                false
            );

            // Face the direction defined
            // by the patrol point
            FacePatrolDirection();

            waitTimer +=
                Time.deltaTime;

            if (waitTimer >=
                patrolWaitTime)
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
            // Walk to patrol point
            SetMovementAnimation(
                true,
                false
            );
        }
    }

    void FacePatrolDirection()
    {
        if (patrolPoints == null ||
            patrolPoints.Length == 0)
        {
            return;
        }

        Transform point =
            patrolPoints[currentPatrolPoint];

        if (point == null)
            return;

        Vector3 direction =
            point.forward;

        direction.y = 0f;

        if (direction.sqrMagnitude >
            0.001f)
        {
            Quaternion targetRotation =
                Quaternion.LookRotation(
                    direction
                );

            transform.rotation =
                Quaternion.Slerp(
                    transform.rotation,
                    targetRotation,
                    patrolTurnSpeed *
                    Time.deltaTime
                );
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

        if (patrolPoints[
            currentPatrolPoint] == null)
        {
            return;
        }

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

        ResetBurst();

        SetCombatAnimation(false);

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

    void HandlePlayerDeath()
    {
        chasingPlayer = false;
        alertTimer = 0f;

        ResetBurst();

        if (AgentReady())
        {
            agent.isStopped = true;
            agent.ResetPath();
        }

        SetMovementAnimation(
            false,
            false
        );

        SetCombatAnimation(false);

        if (animator != null)
        {
            animator.ResetTrigger(
                "Shoot"
            );
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

    void SetCombatAnimation(
        bool combat
    )
    {
        if (animator != null)
        {
            animator.SetBool(
                "IsCombat",
                combat
            );
        }
    }

    bool AgentReady()
    {
        return agent != null &&
               agent.enabled &&
               agent.isOnNavMesh;
    }

    public void AlertPlayer()
    {
        if (playerHealth != null &&
            playerHealth.IsDead)
        {
            return;
        }

        chasingPlayer = true;

        alertTimer =
            alertMemoryDuration;

        waitTimer = 0f;

        SetCombatAnimation(true);

        if (player == null)
            return;

        float distanceToPlayer =
            Vector3.Distance(
                transform.position,
                player.position
            );

        if (AgentReady())
        {
            if (distanceToPlayer >
                attackRange)
            {
                agent.isStopped = false;
                agent.speed = chaseSpeed;

                agent.SetDestination(
                    player.position
                );

                SetMovementAnimation(
                    false,
                    true
                );
            }
            else
            {
                agent.isStopped = true;

                SetMovementAnimation(
                    false,
                    false
                );
            }
        }
    }
}