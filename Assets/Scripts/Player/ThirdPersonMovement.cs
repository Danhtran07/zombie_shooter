using UnityEngine;

public sealed class ThirdPersonMovement
{
    private readonly Transform actor;
    private readonly Rigidbody body;
    private readonly ThirdPersonCamera camera;
    private readonly float speed;
    private readonly float rotationSpeed;
    private readonly bool allowJump;
    private readonly float jumpForce;

    private Vector3 moveDirection;
    private Vector3 aimDirection;
    private bool hasAimDirection;

    public ThirdPersonMovement(
        Transform actor,
        Rigidbody body,
        ThirdPersonCamera camera,
        float speed,
        float rotationSpeed,
        bool allowJump,
        float jumpForce)
    {
        this.actor = actor;
        this.body = body;
        this.camera = camera;
        this.speed = speed;
        this.rotationSpeed = rotationSpeed;
        this.allowJump = allowJump;
        this.jumpForce = jumpForce;

        body.useGravity = true;
        body.isKinematic = false;
        body.interpolation = RigidbodyInterpolation.Interpolate;
        body.collisionDetectionMode = CollisionDetectionMode.Continuous;
        body.constraints =
            RigidbodyConstraints.FreezeRotationX |
            RigidbodyConstraints.FreezeRotationZ;
    }

    public void SetAimDirection(Vector3 direction)
    {
        direction.y = 0f;

        if (direction.sqrMagnitude <= 0.001f)
        {
            hasAimDirection = false;
            return;
        }

        aimDirection = direction.normalized;
        hasAimDirection = true;
    }

    public void ClearAimDirection()
    {
        hasAimDirection = false;
    }

    public void Apply(Vector2 input, bool jumpRequested)
    {
        moveDirection = CalculateMoveDirection(input);
        Move();
        Rotate();

        if (jumpRequested)
        {
            Jump();
        }
    }

    private Vector3 CalculateMoveDirection(Vector2 input)
    {
        if (camera == null)
        {
            return actor.forward * input.y + actor.right * input.x;
        }

        Vector3 forward = camera.GetForward();
        Vector3 right = camera.GetRight();
        return Vector3.ClampMagnitude(forward * input.y + right * input.x, 1f);
    }

    private void Move()
    {
        Vector3 velocity = body.velocity;
        Vector3 horizontalVelocity = moveDirection * speed;
        body.velocity = new Vector3(
            horizontalVelocity.x,
            velocity.y,
            horizontalVelocity.z
        );
    }

    private void Rotate()
    {
        Vector3 facingDirection = hasAimDirection
            ? aimDirection
            : moveDirection;

        if (facingDirection.sqrMagnitude < 0.001f)
        {
            return;
        }

        Quaternion targetRotation =
            Quaternion.LookRotation(facingDirection, Vector3.up);
        Quaternion newRotation = Quaternion.RotateTowards(
            body.rotation,
            targetRotation,
            rotationSpeed * Time.fixedDeltaTime
        );

        body.MoveRotation(newRotation);
    }

    private void Jump()
    {
        if (!allowJump || !Physics.Raycast(
                actor.position + Vector3.up * 0.1f,
                Vector3.down,
                1.2f))
        {
            return;
        }

        Vector3 velocity = body.velocity;
        velocity.y = 0f;
        body.velocity = velocity;
        body.AddForce(Vector3.up * jumpForce, ForceMode.VelocityChange);
    }
}