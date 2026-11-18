using UnityEngine;
using UnityEngine.InputSystem;

[RequireComponent(typeof(AudioSource))]
public class GunShoot : MonoBehaviour
{
    [Header("References")]
    public Camera playerCamera;

    [Header("Gun Settings")]
    public float damage = 25f;
    public float range = 100f;

    [Header("Enemy Hearing")]
    public float hearingRange = 45f;

    private AudioSource gunAudioSource;


    void Start()
    {
        gunAudioSource =
            GetComponent<AudioSource>();
    }


    void Update()
    {
        Mouse mouse = Mouse.current;

        if (mouse == null)
            return;

        if (mouse.leftButton.wasPressedThisFrame)
        {
            Shoot();
        }
    }


    void Shoot()
    {
        // -----------------------------
        // PLAY GUNSHOT SOUND
        // -----------------------------

        if (gunAudioSource != null)
        {
            gunAudioSource.Play();
        }


        // -----------------------------
        // ALERT NEARBY ENEMIES
        // -----------------------------

        AlertNearbyEnemies();


        // -----------------------------
        // SHOOTING RAY
        // -----------------------------

        Ray ray = new Ray(
            playerCamera.transform.position,
            playerCamera.transform.forward
        );

        RaycastHit hit;


        if (Physics.Raycast(
            ray,
            out hit,
            range))
        {
            Debug.Log(
                "Hit: " +
                hit.transform.name
            );


            // -------------------------
            // ALERT HIT ENEMY
            // -------------------------

            EnemyAI enemyAI =
                hit.transform
                    .GetComponentInParent<EnemyAI>();

            if (enemyAI != null)
            {
                enemyAI.AlertPlayer();
            }


            // -------------------------
            // APPLY DAMAGE
            // -------------------------

            EnemyHealth enemyHealth =
                hit.transform
                    .GetComponentInParent<EnemyHealth>();

            if (enemyHealth != null)
            {
                enemyHealth.TakeDamage(
                    damage
                );
            }


            Debug.DrawRay(
                ray.origin,
                ray.direction *
                hit.distance,
                Color.red,
                1f
            );
        }
        else
        {
            Debug.DrawRay(
                ray.origin,
                ray.direction *
                range,
                Color.green,
                1f
            );
        }
    }


    // ========================================================
    // ENEMY HEARING
    // ========================================================

    void AlertNearbyEnemies()
    {
        Collider[] nearbyObjects =
            Physics.OverlapSphere(
                playerCamera.transform.position,
                hearingRange
            );


        foreach (Collider nearbyObject
                 in nearbyObjects)
        {
            EnemyAI enemy =
                nearbyObject
                    .GetComponentInParent<EnemyAI>();

            if (enemy != null)
            {
                enemy.AlertPlayer();
            }
        }
    }
}