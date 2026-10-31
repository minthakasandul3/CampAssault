using UnityEngine;

public class PlayerPush : MonoBehaviour
{
    [Header("Push Settings")]
    public float pushPower = 2.5f;

    void OnControllerColliderHit(ControllerColliderHit hit)
    {
        Rigidbody body =
            hit.collider.attachedRigidbody;

        // Ignore objects without Rigidbody
        if (body == null)
            return;

        // Ignore kinematic objects
        if (body.isKinematic)
            return;

        // Don't push objects vertically
        Vector3 pushDirection =
            new Vector3(
                hit.moveDirection.x,
                0f,
                hit.moveDirection.z
            );

        body.AddForce(
            pushDirection * pushPower,
            ForceMode.Impulse
        );
    }
}