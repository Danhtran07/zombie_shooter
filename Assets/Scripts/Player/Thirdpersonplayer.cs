using UnityEngine;

[RequireComponent(typeof(Rigidbody))]
[RequireComponent(typeof(CapsuleCollider))]
public class ThirdPersonController : MonoBehaviour
{
    [Header("Movement")]
    [SerializeField] private float sprintSpeed = 8f;

    [Header("Rotation")]
    [SerializeField] private float rotationSpeed = 720f;

    [Header("References")]
    [SerializeField] private ThirdPersonCamera thirdPersonCamera;

    [Header("Jump")]
    [SerializeField] private bool allowJump = false;
    [SerializeField] private float jumpForce = 5f;

    private ThirdPersonInput input;
    private ThirdPersonMovement movement;
    private bool jumpRequested;

    private void Awake()
    {
        Rigidbody rb = GetComponent<Rigidbody>();

        if (thirdPersonCamera == null)
        {
            thirdPersonCamera =
                FindObjectOfType<ThirdPersonCamera>();
        }

        input = new ThirdPersonInput();
        movement = new ThirdPersonMovement(
            transform,
            rb,
            thirdPersonCamera,
            sprintSpeed,
            rotationSpeed,
            allowJump,
            jumpForce
        );
    }

    public void SetAimDirection(Vector3 direction)
    {
        movement?.SetAimDirection(direction);
    }

    public void ClearAimDirection()
    {
        movement?.ClearAimDirection();
    }

    private void Update()
    {
        jumpRequested = allowJump && input.IsJumpPressed();

        if (!allowJump)
            jumpRequested = false;
    }

    private void FixedUpdate()
    {
        movement.Apply(input.ReadMovement(), jumpRequested);
        jumpRequested = false;
    }
}