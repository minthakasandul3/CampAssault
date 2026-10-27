using UnityEngine;
using UnityEngine.InputSystem;

[RequireComponent(typeof(CharacterController))]
public class FirstPersonController : MonoBehaviour
{
    [Header("Movement")]
    public float walkSpeed = 5f;
    public float runSpeed = 8f;

    [Header("Jump")]
    public float jumpHeight = 2f;
    public float gravity = -9.81f;

    private CharacterController controller;
    private Vector3 verticalVelocity;

    void Awake()
    {
        controller = GetComponent<CharacterController>();
    }

    void Update()
    {
        Keyboard keyboard = Keyboard.current;

        if (keyboard == null)
            return;

        // Check whether the player is touching the ground
        bool isGrounded = controller.isGrounded;

        // Keep the player attached to the ground
        if (isGrounded && verticalVelocity.y < 0f)
        {
            verticalVelocity.y = -2f;
        }

        // -------------------------
        // Movement input
        // -------------------------

        float x = 0f;
        float z = 0f;

        if (keyboard.aKey.isPressed)
            x -= 1f;

        if (keyboard.dKey.isPressed)
            x += 1f;

        if (keyboard.sKey.isPressed)
            z -= 1f;

        if (keyboard.wKey.isPressed)
            z += 1f;

        // Prevent diagonal movement from being faster
        Vector2 input =
            Vector2.ClampMagnitude(new Vector2(x, z), 1f);

        Vector3 movement =
            transform.right * input.x +
            transform.forward * input.y;

        // -------------------------
        // Running
        // -------------------------

        bool isRunning =
            keyboard.leftShiftKey.isPressed ||
            keyboard.rightShiftKey.isPressed;

        float currentSpeed =
            isRunning ? runSpeed : walkSpeed;

        // -------------------------
        // Jumping
        // -------------------------

        if (isGrounded &&
            keyboard.spaceKey.wasPressedThisFrame)
        {
            verticalVelocity.y =
                Mathf.Sqrt(jumpHeight * -2f * gravity);
        }

        // -------------------------
        // Gravity
        // -------------------------

        verticalVelocity.y += gravity * Time.deltaTime;

        // Combine horizontal and vertical movement
        Vector3 finalMovement =
            movement * currentSpeed +
            verticalVelocity;

        // Move the Character Controller once per frame
        controller.Move(finalMovement * Time.deltaTime);
    }
}