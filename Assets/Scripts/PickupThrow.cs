using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

public class PickupThrow : MonoBehaviour
{
    [Header("References")]
    public Camera playerCamera;
    public Transform holdPoint;

    [Header("Weapon / UI")]
    public GameObject gunObject;
    public GameObject crosshair;
    public GunShoot gunShoot;

    [Header("Pickup Settings")]
    public float pickupRange = 3f;
    public float pickupRadius = 0.22f;

    // Helps when the player is extremely close.
    public float closePickupRange = 1.2f;

    [Header("Throw Settings")]
    public float throwForce = 8f;

    private Rigidbody heldBody;

    // Only colliders that WE disable while holding
    // are stored here.
    private List<Collider> collidersDisabledForHold =
        new List<Collider>();


    void Update()
    {
        Keyboard keyboard = Keyboard.current;

        if (keyboard == null)
            return;


        // F = Pick up / Drop
        if (keyboard.fKey.wasPressedThisFrame)
        {
            if (heldBody == null)
            {
                TryPickup();
            }
            else
            {
                DropObject();
            }
        }


        // G = Throw
        if (keyboard.gKey.wasPressedThisFrame &&
            heldBody != null)
        {
            ThrowObject();
        }
    }


    // ========================================================
    // PICK UP
    // ========================================================

    void TryPickup()
    {
        Rigidbody body =
            FindPickupObject();

        if (body == null)
            return;


        heldBody = body;

        heldBody.linearVelocity =
            Vector3.zero;

        heldBody.angularVelocity =
            Vector3.zero;


        // ----------------------------------------
        // SAVE ONLY CURRENTLY ENABLED COLLIDERS
        // ----------------------------------------

        collidersDisabledForHold.Clear();

        Collider[] objectColliders =
            heldBody.GetComponentsInChildren<Collider>();

        foreach (Collider col in objectColliders)
        {
            if (col != null &&
                col.enabled)
            {
                collidersDisabledForHold.Add(col);

                col.enabled = false;
            }
        }


        // ----------------------------------------
        // DISABLE PHYSICS WHILE HELD
        // ----------------------------------------

        heldBody.useGravity = false;
        heldBody.isKinematic = true;


        // ----------------------------------------
        // MOVE TO HOLD POINT
        // ----------------------------------------

        heldBody.transform.SetParent(
            holdPoint
        );

        heldBody.transform.localPosition =
            Vector3.zero;

        heldBody.transform.localRotation =
            Quaternion.identity;


        SetWeaponVisible(false);
    }


    // ========================================================
    // FIND PICKUP OBJECT
    // ========================================================

    Rigidbody FindPickupObject()
    {
        if (playerCamera == null)
            return null;


        Vector3 origin =
            playerCamera.transform.position;

        Vector3 direction =
            playerCamera.transform.forward;


        // ====================================================
        // NORMAL / MEDIUM DISTANCE PICKUP
        //
        // SphereCastAll instead of SphereCast.
        // This prevents another collider from automatically
        // stopping us from finding the Throwable object.
        // ====================================================

        RaycastHit[] hits =
            Physics.SphereCastAll(
                origin,
                pickupRadius,
                direction,
                pickupRange,
                Physics.AllLayers,
                QueryTriggerInteraction.Ignore
            );


        System.Array.Sort(
            hits,
            (a, b) =>
                a.distance.CompareTo(b.distance)
        );


        foreach (RaycastHit hit in hits)
        {
            if (hit.collider == null)
                continue;


            // Ignore Player's own colliders.
            if (IsPlayerCollider(hit.collider))
                continue;


            Rigidbody body =
                GetThrowableBody(
                    hit.collider
                );


            if (body != null)
            {
                return body;
            }
        }


        // ====================================================
        // VERY CLOSE PICKUP
        //
        // When the can is almost touching the player,
        // search around the player's lower body instead of
        // around camera height.
        // ====================================================

        Vector3 closeCheckPosition =
            transform.position +
            Vector3.up * 0.6f;


        Collider[] closeColliders =
            Physics.OverlapSphere(
                closeCheckPosition,
                closePickupRange,
                Physics.AllLayers,
                QueryTriggerInteraction.Ignore
            );


        Rigidbody bestBody = null;

        float bestScore =
            Mathf.Infinity;


        foreach (Collider col in closeColliders)
        {
            if (col == null)
                continue;


            if (IsPlayerCollider(col))
                continue;


            Rigidbody body =
                GetThrowableBody(col);

            if (body == null)
                continue;


            Vector3 targetPosition =
                col.bounds.center;


            Vector3 toObject =
                targetPosition -
                playerCamera.transform.position;


            float distance =
                toObject.magnitude;


            if (distance <= 0.001f)
                return body;


            Vector3 toObjectDirection =
                toObject.normalized;


            float forwardAmount =
                Vector3.Dot(
                    playerCamera.transform.forward,
                    toObjectDirection
                );


            // Must be somewhere in front of player.
            if (forwardAmount < 0.15f)
                continue;


            // Prefer an object closer to the centre
            // of the player's view.
            float angle =
                Vector3.Angle(
                    playerCamera.transform.forward,
                    toObjectDirection
                );


            float score =
                angle +
                distance * 5f;


            if (score < bestScore)
            {
                bestScore = score;

                bestBody = body;
            }
        }


        return bestBody;
    }


    // ========================================================
    // THROWABLE CHECK
    // ========================================================

    Rigidbody GetThrowableBody(
        Collider col
    )
    {
        if (col == null)
            return null;


        Rigidbody body =
            col.attachedRigidbody;


        if (body == null)
            return null;


        // Tag can be on collider object
        // OR Rigidbody/root object.
        if (col.CompareTag("Throwable") ||
            body.CompareTag("Throwable"))
        {
            return body;
        }


        return null;
    }


    // ========================================================
    // IGNORE PLAYER COLLIDERS
    // ========================================================

    bool IsPlayerCollider(
        Collider col
    )
    {
        if (col == null)
            return false;


        Transform colTransform =
            col.transform;


        if (colTransform == transform)
            return true;


        if (colTransform.IsChildOf(transform))
            return true;


        return false;
    }


    // ========================================================
    // DROP
    // ========================================================

    void DropObject()
    {
        if (heldBody == null)
            return;


        Rigidbody bodyToDrop =
            heldBody;


        bodyToDrop.transform.SetParent(
            null
        );


        bodyToDrop.isKinematic = false;
        bodyToDrop.useGravity = true;


        RestoreHeldColliders();


        heldBody = null;


        SetWeaponVisible(true);
    }


    // ========================================================
    // THROW
    // ========================================================

    void ThrowObject()
    {
        if (heldBody == null)
            return;


        Rigidbody bodyToThrow =
            heldBody;


        bodyToThrow.transform.SetParent(
            null
        );


        bodyToThrow.isKinematic = false;
        bodyToThrow.useGravity = true;


        RestoreHeldColliders();


        heldBody = null;


        bodyToThrow.AddForce(
            playerCamera.transform.forward *
            throwForce,
            ForceMode.Impulse
        );


        SetWeaponVisible(true);
    }


    // ========================================================
    // RESTORE COLLIDERS
    // ========================================================

    void RestoreHeldColliders()
    {
        foreach (Collider col
                 in collidersDisabledForHold)
        {
            if (col != null)
            {
                col.enabled = true;
            }
        }


        collidersDisabledForHold.Clear();
    }


    // ========================================================
    // WEAPON / CROSSHAIR
    // ========================================================

    void SetWeaponVisible(
        bool visible
    )
    {
        if (gunObject != null)
        {
            gunObject.SetActive(
                visible
            );
        }


        if (crosshair != null)
        {
            crosshair.SetActive(
                visible
            );
        }


        if (gunShoot != null)
        {
            gunShoot.enabled =
                visible;
        }
    }
}