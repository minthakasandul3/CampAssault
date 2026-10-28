using UnityEngine;
using UnityEngine.InputSystem;

public class GunShoot : MonoBehaviour
{
    [Header("References")]
    public Camera playerCamera;

    [Header("Gun Settings")]
    public float damage = 25f;
    public float range = 100f;

    [Header("Enemy Hearing")]
    public float hearingRange = 45f;

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
        // Alert enemies that hear the gunshot
        AlertNearbyEnemies();

        Ray ray = new Ray(
            playerCamera.transform.position,
            playerCamera.transform.forward
        );

        RaycastHit hit;

        if (Physics.Raycast(ray, out hit, range))
        {
            Debug.Log(
                "Hit: " + hit.transform.name
            );

            // Find the enemy AI first
            EnemyAI enemyAI =
                hit.transform.GetComponentInParent<EnemyAI>();

            // Alert the enemy BEFORE applying damage
            if (enemyAI != null)
            {
                enemyAI.AlertPlayer();
            }

            // Then apply damage
            EnemyHealth enemyHealth =
                hit.transform.GetComponentInParent<EnemyHealth>();

            if (enemyHealth != null)
            {
                enemyHealth.TakeDamage(damage);
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
    }

    void AlertNearbyEnemies()
    {
        Collider[] nearbyObjects =
            Physics.OverlapSphere(
                playerCamera.transform.position,
                hearingRange
            );

        foreach (Collider nearbyObject in nearbyObjects)
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