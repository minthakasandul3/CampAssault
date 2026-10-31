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

    [Header("Settings")]
    public float pickupRange = 3f;
    public float throwForce = 8f;

    private Rigidbody heldBody;
    private Collider heldCollider;

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

    void TryPickup()
    {
        Ray ray = new Ray(
            playerCamera.transform.position,
            playerCamera.transform.forward
        );

        RaycastHit hit;

        if (Physics.Raycast(ray, out hit, pickupRange))
        {
            if (!hit.collider.CompareTag("Throwable"))
                return;

            Rigidbody body =
                hit.collider.attachedRigidbody;

            if (body == null)
                return;

            heldBody = body;
            heldCollider = hit.collider;

            heldBody.linearVelocity = Vector3.zero;
            heldBody.angularVelocity = Vector3.zero;

            heldBody.useGravity = false;
            heldBody.isKinematic = true;

            if (heldCollider != null)
            {
                heldCollider.enabled = false;
            }

            heldBody.transform.SetParent(holdPoint);

            heldBody.transform.localPosition =
                Vector3.zero;

            heldBody.transform.localRotation =
                Quaternion.identity;

            SetWeaponVisible(false);
        }
    }

    void DropObject()
    {
        heldBody.transform.SetParent(null);

        heldBody.isKinematic = false;
        heldBody.useGravity = true;

        if (heldCollider != null)
        {
            heldCollider.enabled = true;
        }

        heldBody = null;
        heldCollider = null;

        SetWeaponVisible(true);
    }

    void ThrowObject()
    {
        Rigidbody bodyToThrow =
            heldBody;

        Collider colliderToEnable =
            heldCollider;

        bodyToThrow.transform.SetParent(null);

        bodyToThrow.isKinematic = false;
        bodyToThrow.useGravity = true;

        if (colliderToEnable != null)
        {
            colliderToEnable.enabled = true;
        }

        heldBody = null;
        heldCollider = null;

        bodyToThrow.AddForce(
            playerCamera.transform.forward *
            throwForce,
            ForceMode.Impulse
        );

        SetWeaponVisible(true);
    }

    void SetWeaponVisible(bool visible)
    {
        if (gunObject != null)
        {
            gunObject.SetActive(visible);
        }

        if (crosshair != null)
        {
            crosshair.SetActive(visible);
        }

        if (gunShoot != null)
        {
            gunShoot.enabled = visible;
        }
    }
}