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
    public float detectionRange = 8f;
    public float sightRange = 70f;
    public float fieldOfView = 110f;
    public float stopChasingRange = 90f;

    [Header("Alert")]
    public float alertMemoryDuration = 12f;

    [Header("Movement Speeds")]
    public float patrolSpeed = 1.8f;
    public float chaseSpeed = 3.5f;

    [Header("Patrol")]
    public float patrolWaitTime = 4f;
    public float patrolTurnSpeed = 8f;
    public float patrolTurnTolerance = 2f;
    public float patrolStoppingDistance = 0.15f;
    public float arrivalRotationLockDistance = 0.6f;

    [Header("Turning")]
    public float chaseTurnSpeed = 180f;
    public float combatTurnSpeed = 180f;
    public float attackFacingTolerance = 6f;

    [Header("Animation")]
    public float movementAnimationDamping = 0.08f;

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

    private bool waitingAtPatrolPoint = false;
    private bool arrivalRotationLocked = false;

    private bool isBursting = false;
    private int shotsFiredInBurst = 0;
    private float shotTimer = 0f;
    private float burstTimer = 0f;

    private float desiredMoveSpeed = 0f;


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
            agent.stoppingDistance =
                patrolStoppingDistance;

            agent.autoBraking = true;
            agent.updateRotation = true;
        }

        desiredMoveSpeed = 0f;

        SetMoveSpeedImmediate(0f);
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

        bool hasLineOfSight =
            HasLineOfSightToPlayer();


        // ========================================
        // DETECTION
        // ========================================

        if (distanceToPlayer <= detectionRange ||
            canSeePlayer)
        {
            chasingPlayer = true;

            alertTimer =
                alertMemoryDuration;

            waitingAtPatrolPoint = false;
            arrivalRotationLocked = false;

            waitTimer = 0f;
        }


        // ========================================
        // STOP CHASING
        // ========================================

        if (chasingPlayer &&
            distanceToPlayer > stopChasingRange &&
            alertTimer <= 0f)
        {
            StopChasing();
        }


        // ========================================
        // STATE DECISION
        // ========================================

        if (chasingPlayer)
        {
            // Attack only when close enough
            // AND there is a clear path to player.
            if (distanceToPlayer <= attackRange &&
                hasLineOfSight)
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


        UpdateMovementAnimation();
    }


    // ========================================================
    // VISUAL DETECTION
    // ========================================================

    bool CanSeePlayer()
    {
        if (player == null)
            return false;

        Vector3 enemyEyePosition =
            GetEnemyEyePosition();

        Vector3 playerTargetPosition =
            GetPlayerTargetPosition();

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

        return HasClearRayToPlayer(
            enemyEyePosition,
            playerTargetPosition
        );
    }


    // ========================================================
    // LINE OF SIGHT
    // ========================================================

    bool HasLineOfSightToPlayer()
    {
        if (player == null)
            return false;

        Vector3 enemyEyePosition =
            GetEnemyEyePosition();

        Vector3 playerTargetPosition =
            GetPlayerTargetPosition();

        return HasClearRayToPlayer(
            enemyEyePosition,
            playerTargetPosition
        );
    }


    Vector3 GetEnemyEyePosition()
    {
        return transform.position +
               Vector3.up * 1.5f;
    }


    Vector3 GetPlayerTargetPosition()
    {
        Vector3 targetPosition =
            player.position;

        CharacterController playerController =
            player.GetComponent<CharacterController>();

        if (playerController != null)
        {
            return playerController.bounds.center;
        }

        Collider playerCollider =
            player.GetComponentInChildren<Collider>();

        if (playerCollider != null)
        {
            targetPosition =
                playerCollider.bounds.center;
        }

        return targetPosition;
    }


    bool HasClearRayToPlayer(
        Vector3 start,
        Vector3 target
    )
    {
        Vector3 direction =
            target - start;

        float distance =
            direction.magnitude;

        if (distance <= 0.001f)
            return true;

        direction.Normalize();

        RaycastHit[] hits =
            Physics.RaycastAll(
                start,
                direction,
                distance,
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
            // Ignore this enemy.
            if (hit.transform == transform ||
                hit.transform.IsChildOf(transform))
            {
                continue;
            }

            // Player reached first.
            if (hit.transform == player ||
                hit.transform.IsChildOf(player))
            {
                return true;
            }

            // Obstacle blocks player.
            return false;
        }

        return false;
    }


    // ========================================================
    // CHASE
    // ========================================================

    void ChasePlayer()
    {
        ResetBurst();

        waitingAtPatrolPoint = false;
        arrivalRotationLocked = false;

        waitTimer = 0f;

        if (!AgentReady())
            return;


        // ----------------------------------------
        // ANIMATION
        // ----------------------------------------

        desiredMoveSpeed =
            chaseSpeed;

        SetMoveSpeedImmediate(
            chaseSpeed
        );

        SetCombatAnimation(false);


        // ----------------------------------------
        // MOVEMENT
        // ----------------------------------------

        agent.isStopped = false;
        agent.speed = chaseSpeed;

        // IMPORTANT:
        // NavMesh still calculates the path,
        // but our script controls rotation.
        agent.updateRotation = false;

        agent.SetDestination(
            player.position
        );


        // Rotate towards the NavMesh's desired
        // movement direction.
        FaceChaseDirection();
    }


    // ========================================================
    // CHASE ROTATION
    // ========================================================

    void FaceChaseDirection()
    {
        if (!AgentReady())
            return;

        Vector3 direction =
            agent.desiredVelocity;

        direction.y = 0f;

        if (direction.sqrMagnitude <= 0.01f)
            return;

        Quaternion targetRotation =
            Quaternion.LookRotation(
                direction.normalized
            );

        transform.rotation =
            Quaternion.RotateTowards(
                transform.rotation,
                targetRotation,
                chaseTurnSpeed *
                Time.deltaTime
            );
    }


    // ========================================================
    // ATTACK
    // ========================================================

    void AttackPlayer()
    {
        waitingAtPatrolPoint = false;
        arrivalRotationLocked = false;

        waitTimer = 0f;

        if (AgentReady())
        {
            agent.isStopped = true;

            agent.velocity =
                Vector3.zero;

            // Script controls rotation.
            agent.updateRotation = false;
        }


        desiredMoveSpeed = 0f;

        SetCombatAnimation(true);


        // ----------------------------------------
        // TURN TOWARDS PLAYER FIRST
        // ----------------------------------------

        bool facingPlayer =
            FacePlayer();


        // Do not shoot while turning.
        if (!facingPlayer)
        {
            ResetBurst();
            return;
        }


        // Do not shoot through obstacles.
        if (!HasLineOfSightToPlayer())
        {
            ResetBurst();
            return;
        }


        // ----------------------------------------
        // SHOOT
        // ----------------------------------------

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


    // ========================================================
    // COMBAT ROTATION
    // ========================================================

    bool FacePlayer()
    {
        if (player == null)
            return false;

        Vector3 targetPosition =
            GetPlayerTargetPosition();

        Vector3 direction =
            targetPosition -
            transform.position;

        direction.y = 0f;

        if (direction.sqrMagnitude <= 0.001f)
        {
            return true;
        }

        Quaternion targetRotation =
            Quaternion.LookRotation(
                direction.normalized
            );

        transform.rotation =
            Quaternion.RotateTowards(
                transform.rotation,
                targetRotation,
                combatTurnSpeed *
                Time.deltaTime
            );

        float remainingAngle =
            Quaternion.Angle(
                transform.rotation,
                targetRotation
            );

        return remainingAngle <=
               attackFacingTolerance;
    }


    // ========================================================
    // BURST FIRE
    // ========================================================

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


    // ========================================================
    // PATROL
    // ========================================================

    void Patrol()
    {
        ResetBurst();

        SetCombatAnimation(false);

        if (patrolPoints == null ||
            patrolPoints.Length == 0)
        {
            desiredMoveSpeed = 0f;
            return;
        }

        if (!AgentReady())
        {
            desiredMoveSpeed = 0f;
            return;
        }

        agent.speed =
            patrolSpeed;

        agent.stoppingDistance =
            patrolStoppingDistance;

        agent.autoBraking = true;


        // ----------------------------------------
        // WAITING AT PATROL POINT
        // ----------------------------------------

        if (waitingAtPatrolPoint)
        {
            agent.isStopped = true;

            agent.velocity =
                Vector3.zero;

            agent.updateRotation = false;

            desiredMoveSpeed = 0f;

            bool finishedTurning =
                FacePatrolDirection();

            if (finishedTurning)
            {
                waitTimer +=
                    Time.deltaTime;

                if (waitTimer >=
                    patrolWaitTime)
                {
                    waitTimer = 0f;

                    waitingAtPatrolPoint =
                        false;

                    arrivalRotationLocked =
                        false;

                    currentPatrolPoint++;

                    if (currentPatrolPoint >=
                        patrolPoints.Length)
                    {
                        currentPatrolPoint = 0;
                    }

                    GoToCurrentPatrolPoint();
                }
            }

            return;
        }


        // ----------------------------------------
        // WALKING
        // ----------------------------------------

        agent.isStopped = false;

        desiredMoveSpeed =
            patrolSpeed;

        if (!agent.pathPending)
        {
            // Preserve final approach direction.
            if (!arrivalRotationLocked &&
                agent.remainingDistance <=
                arrivalRotationLockDistance)
            {
                arrivalRotationLocked = true;

                agent.updateRotation = false;
            }
            else if (!arrivalRotationLocked)
            {
                agent.updateRotation = true;
            }


            // Arrival.
            if (agent.remainingDistance <=
                patrolStoppingDistance)
            {
                waitingAtPatrolPoint = true;

                agent.isStopped = true;

                agent.velocity =
                    Vector3.zero;

                agent.ResetPath();

                agent.updateRotation = false;

                desiredMoveSpeed = 0f;

                SetMoveSpeedImmediate(0f);

                waitTimer = 0f;

                return;
            }
        }
    }


    // ========================================================
    // PATROL POINT ROTATION
    // ========================================================

    bool FacePatrolDirection()
    {
        if (patrolPoints == null ||
            patrolPoints.Length == 0)
        {
            return true;
        }

        Transform point =
            patrolPoints[currentPatrolPoint];

        if (point == null)
            return true;

        Vector3 targetDirection =
            point.forward;

        targetDirection.y = 0f;

        if (targetDirection.sqrMagnitude <=
            0.001f)
        {
            return true;
        }

        targetDirection.Normalize();

        Quaternion targetRotation =
            Quaternion.LookRotation(
                targetDirection
            );

        float angle =
            Quaternion.Angle(
                transform.rotation,
                targetRotation
            );

        if (angle <=
            patrolTurnTolerance)
        {
            transform.rotation =
                targetRotation;

            return true;
        }

        transform.rotation =
            Quaternion.RotateTowards(
                transform.rotation,
                targetRotation,
                patrolTurnSpeed *
                Time.deltaTime
            );

        return false;
    }


    // ========================================================
    // GO TO PATROL POINT
    // ========================================================

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

        waitingAtPatrolPoint = false;

        arrivalRotationLocked = false;

        waitTimer = 0f;


        desiredMoveSpeed =
            patrolSpeed;

        SetMoveSpeedImmediate(
            patrolSpeed
        );

        SetCombatAnimation(false);


        agent.stoppingDistance =
            patrolStoppingDistance;

        agent.autoBraking = true;

        // Normal NavMesh rotation during patrol.
        agent.updateRotation = true;

        agent.isStopped = false;

        agent.speed =
            patrolSpeed;

        agent.SetDestination(
            patrolPoints[
                currentPatrolPoint
            ].position
        );
    }


    // ========================================================
    // STOP CHASING
    // ========================================================

    void StopChasing()
    {
        chasingPlayer = false;

        alertTimer = 0f;

        waitingAtPatrolPoint = false;

        arrivalRotationLocked = false;

        waitTimer = 0f;

        ResetBurst();

        if (AgentReady())
        {
            agent.isStopped = false;

            agent.speed =
                patrolSpeed;

            agent.stoppingDistance =
                patrolStoppingDistance;

            agent.autoBraking = true;

            agent.updateRotation = true;
        }

        if (patrolPoints != null &&
            patrolPoints.Length > 0)
        {
            GoToCurrentPatrolPoint();
        }
    }


    // ========================================================
    // PLAYER DEATH
    // ========================================================

    void HandlePlayerDeath()
    {
        chasingPlayer = false;

        alertTimer = 0f;

        waitingAtPatrolPoint = false;

        arrivalRotationLocked = false;

        waitTimer = 0f;

        ResetBurst();

        if (AgentReady())
        {
            agent.isStopped = true;

            agent.velocity =
                Vector3.zero;

            agent.ResetPath();

            agent.updateRotation = false;
        }

        desiredMoveSpeed = 0f;

        SetMoveSpeedImmediate(0f);

        SetCombatAnimation(false);

        if (animator != null)
        {
            animator.ResetTrigger(
                "Shoot"
            );
        }
    }


    // ========================================================
    // BLEND TREE MOVEMENT
    // ========================================================

    void UpdateMovementAnimation()
    {
        if (animator == null)
            return;

        animator.SetFloat(
            "MoveSpeed",
            desiredMoveSpeed,
            movementAnimationDamping,
            Time.deltaTime
        );
    }


    void SetMoveSpeedImmediate(
        float speed
    )
    {
        if (animator == null)
            return;

        animator.SetFloat(
            "MoveSpeed",
            speed
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


    // ========================================================
    // ALERT FROM PLAYER SHOT
    // ========================================================

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

        waitingAtPatrolPoint = false;

        arrivalRotationLocked = false;

        waitTimer = 0f;

        if (player == null)
            return;

        float distanceToPlayer =
            Vector3.Distance(
                transform.position,
                player.position
            );

        if (!AgentReady())
            return;

        bool hasLineOfSight =
            HasLineOfSightToPlayer();


        // ----------------------------------------
        // ALREADY CLOSE ENOUGH
        // ----------------------------------------

        if (distanceToPlayer <= attackRange &&
            hasLineOfSight)
        {
            desiredMoveSpeed = 0f;

            agent.isStopped = true;

            agent.velocity =
                Vector3.zero;

            agent.updateRotation = false;

            SetCombatAnimation(true);

            return;
        }


        // ----------------------------------------
        // CHASE PLAYER
        // ----------------------------------------

        desiredMoveSpeed =
            chaseSpeed;

        SetMoveSpeedImmediate(
            chaseSpeed
        );

        SetCombatAnimation(false);

        agent.isStopped = false;

        agent.speed =
            chaseSpeed;

        // Script controls chase rotation.
        agent.updateRotation = false;

        agent.SetDestination(
            player.position
        );
    }
}