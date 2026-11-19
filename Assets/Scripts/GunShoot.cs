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

    [Header("Automatic Fire")]
    public float fireRate = 9f;

    [Header("Magazine")]
    public int magazineSize = 30;
    public float reloadTime = 2f;

    [Header("Enemy Hearing")]
    public float hearingRange = 45f;

    private AudioSource gunAudioSource;

    private int currentAmmo;
    private float nextFireTime = 0f;

    private bool isReloading = false;
    private float reloadTimer = 0f;


    void Start()
    {
        gunAudioSource = GetComponent<AudioSource>();

        currentAmmo = magazineSize;
    }


    void Update()
    {
        Mouse mouse = Mouse.current;

        if (mouse == null)
            return;


        // ========================================
        // RELOADING
        // ========================================

        if (isReloading)
        {
            reloadTimer -= Time.deltaTime;

            if (reloadTimer <= 0f)
            {
                FinishReload();
            }

            return;
        }


        // ========================================
        // START AUTOMATIC RELOAD
        // ========================================

        if (currentAmmo <= 0)
        {
            StartReload();
            return;
        }


        // ========================================
        // AUTOMATIC FIRE
        // ========================================

        if (mouse.leftButton.isPressed &&
            Time.time >= nextFireTime)
        {
            nextFireTime =
                Time.time + (1f / fireRate);

            FireShot();
        }
    }


    void FireShot()
    {
        if (currentAmmo <= 0 ||
            isReloading)
        {
            return;
        }


        // ----------------------------------------
        // USE ONE BULLET
        // ----------------------------------------

        currentAmmo--;


        // ----------------------------------------
        // GUNSHOT SOUND
        // ----------------------------------------

        if (gunAudioSource != null &&
            gunAudioSource.clip != null)
        {
            gunAudioSource.PlayOneShot(
                gunAudioSource.clip
            );
        }


        // ----------------------------------------
        // ALERT NEARBY ENEMIES
        // ----------------------------------------

        AlertNearbyEnemies();


        // ----------------------------------------
        // SHOOTING RAY
        // ----------------------------------------

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


            // ------------------------------------
            // ALERT ENEMY THAT WAS HIT
            // ------------------------------------

            EnemyAI enemyAI =
                hit.transform.GetComponentInParent<EnemyAI>();

            if (enemyAI != null)
            {
                enemyAI.AlertPlayer();
            }


            // ------------------------------------
            // DAMAGE ENEMY
            // ------------------------------------

            EnemyHealth enemyHealth =
                hit.transform.GetComponentInParent<EnemyHealth>();

            if (enemyHealth != null)
            {
                enemyHealth.TakeDamage(
                    damage
                );
            }


            Debug.DrawRay(
                ray.origin,
                ray.direction * hit.distance,
                Color.red,
                1f
            );
        }
        else
        {
            Debug.DrawRay(
                ray.origin,
                ray.direction * range,
                Color.green,
                1f
            );
        }


        // ----------------------------------------
        // MAGAZINE EMPTY
        // ----------------------------------------

        if (currentAmmo <= 0)
        {
            StartReload();
        }
    }


    // ============================================
    // RELOAD
    // ============================================

    void StartReload()
    {
        if (isReloading)
            return;

        isReloading = true;

        reloadTimer = reloadTime;

        Debug.Log("Reloading...");
    }


    void FinishReload()
    {
        currentAmmo = magazineSize;

        isReloading = false;

        reloadTimer = 0f;

        Debug.Log("Reload complete.");
    }


    // ============================================
    // ENEMY HEARING
    // ============================================

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
                nearbyObject.GetComponentInParent<EnemyAI>();

            if (enemy != null)
            {
                enemy.AlertPlayer();
            }
        }
    }
}