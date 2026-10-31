using UnityEngine;
using UnityEngine.InputSystem;

public class GateInteraction : MonoBehaviour
{
    [Header("Gate Movement")]
    public Vector3 openOffset = new Vector3(14f, 0f, 0f);
    public float moveSpeed = 3f;

    [Header("Interaction")]
    public Transform player;
    public float interactionDistance = 3f;

    private Vector3 closedPosition;
    private Vector3 openPosition;

    private bool isOpen = false;

    private Collider gateCollider;

    void Start()
    {
        closedPosition = transform.position;

        openPosition =
            closedPosition + openOffset;

        gateCollider =
            GetComponent<Collider>();
    }

    void Update()
    {
        MoveGate();

        if (player == null ||
            gateCollider == null)
        {
            return;
        }

        // Find the closest point on the gate
        // to the player.
        Vector3 closestPoint =
            gateCollider.ClosestPoint(
                player.position
            );

        float distance =
            Vector3.Distance(
                player.position,
                closestPoint
            );

        if (distance <= interactionDistance)
        {
            Keyboard keyboard =
                Keyboard.current;

            if (keyboard != null &&
                keyboard.eKey.wasPressedThisFrame)
            {
                isOpen = !isOpen;
            }
        }
    }

    void MoveGate()
    {
        Vector3 targetPosition =
            isOpen
            ? openPosition
            : closedPosition;

        transform.position =
            Vector3.MoveTowards(
                transform.position,
                targetPosition,
                moveSpeed * Time.deltaTime
            );
    }
}