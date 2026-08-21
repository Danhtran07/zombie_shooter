using UnityEngine;

[RequireComponent(typeof(Rigidbody))]
[RequireComponent(typeof(CapsuleCollider))]
public class ThirdPersonController : MonoBehaviour
{
    [Header("Movement")]
    [SerializeField] private float walkSpeed = 5f;
    [SerializeField] private float sprintSpeed = 8f;

    [Header("Rotation")]
    [SerializeField] private float rotationSpeed = 720f;

    [Header("References")]
    [SerializeField] private ThirdPersonCamera thirdPersonCamera;

    [Header("Jump")]
    [SerializeField] private bool allowJump = false;
    [SerializeField] private float jumpForce = 5f;

    private Rigidbody rb;

    private float horizontalInput;
    private float verticalInput;

    private Vector3 moveDirection;

    private bool jumpRequested;

    private void Awake()
    {
        rb = GetComponent<Rigidbody>();

        if (thirdPersonCamera == null)
        {
            thirdPersonCamera =
                FindObjectOfType<ThirdPersonCamera>();
        }

        rb.useGravity = true;

        rb.isKinematic = false;

        rb.interpolation =
            RigidbodyInterpolation.Interpolate;

        rb.collisionDetectionMode =
            CollisionDetectionMode.Continuous;

        // FREEZE TẤT CẢ ROTATION
        rb.constraints =
            RigidbodyConstraints.FreezeRotationX |
            RigidbodyConstraints.FreezeRotationY |
            RigidbodyConstraints.FreezeRotationZ;
    }

    private void Update()
    {
        ReadInput();

        if (allowJump &&
            Input.GetKeyDown(KeyCode.Space))
        {
            jumpRequested = true;
        }
    }

    private void FixedUpdate()
    {
        CalculateMoveDirection();

        Move();

        Rotate();

        if (jumpRequested)
        {
            Jump();

            jumpRequested = false;
        }
    }

    // =========================================================
    // INPUT
    // =========================================================

    private void ReadInput()
    {
        horizontalInput =
            Input.GetAxisRaw("Horizontal");

        verticalInput =
            Input.GetAxisRaw("Vertical");

        Vector2 input =
            new Vector2(
                horizontalInput,
                verticalInput
            );

        if (input.magnitude > 1f)
        {
            input.Normalize();
        }

        horizontalInput = input.x;
        verticalInput = input.y;
    }

    // =========================================================
    // MOVE DIRECTION
    // =========================================================

    private void CalculateMoveDirection()
    {
        if (thirdPersonCamera == null)
        {
            moveDirection =
                transform.forward *
                verticalInput +

                transform.right *
                horizontalInput;

            return;
        }

        Vector3 cameraForward =
            thirdPersonCamera.GetForward();

        Vector3 cameraRight =
            thirdPersonCamera.GetRight();

        // Chỉ lấy hướng XZ
        cameraForward.y = 0f;
        cameraRight.y = 0f;

        if (cameraForward.sqrMagnitude > 0.001f)
        {
            cameraForward.Normalize();
        }

        if (cameraRight.sqrMagnitude > 0.001f)
        {
            cameraRight.Normalize();
        }

        moveDirection =
            cameraForward *
            verticalInput +

            cameraRight *
            horizontalInput;

        if (moveDirection.sqrMagnitude > 1f)
        {
            moveDirection.Normalize();
        }
    }

    // =========================================================
    // MOVE
    // =========================================================

    private void Move()
    {
        float speed =
            Input.GetKey(KeyCode.LeftShift)
                ? sprintSpeed
                : walkSpeed;

        Vector3 velocity =
            rb.velocity;

        Vector3 horizontalVelocity =
            moveDirection * speed;

        rb.velocity =
            new Vector3(
                horizontalVelocity.x,
                velocity.y,
                horizontalVelocity.z
            );
    }

    // =========================================================
    // ROTATE
    // =========================================================

    private void Rotate()
    {
        if (moveDirection.sqrMagnitude < 0.001f)
        {
            return;
        }

        Quaternion targetRotation =
            Quaternion.LookRotation(
                moveDirection,
                Vector3.up
            );

        Quaternion newRotation =
            Quaternion.RotateTowards(
                rb.rotation,
                targetRotation,
                rotationSpeed *
                Time.fixedDeltaTime
            );

        // Giữ nguyên logic Rigidbody
        // Chỉ xoay Y
        rb.MoveRotation(newRotation);
    }

    // =========================================================
    // JUMP
    // =========================================================

    private void Jump()
    {
        if (!allowJump)
        {
            return;
        }

        bool grounded =
            Physics.Raycast(
                transform.position +
                Vector3.up * 0.1f,

                Vector3.down,

                1.2f
            );

        if (!grounded)
        {
            return;
        }

        Vector3 velocity =
            rb.velocity;

        velocity.y = 0f;

        rb.velocity =
            velocity;

        rb.AddForce(
            Vector3.up *
            jumpForce,

            ForceMode.VelocityChange
        );
    }
}