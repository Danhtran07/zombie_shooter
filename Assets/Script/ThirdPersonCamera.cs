using UnityEngine;

public class ThirdPersonCamera : MonoBehaviour
{
    [Header("Target")]
    [SerializeField] private Transform target;

    [Header("Distance")]
    [SerializeField] private float distance = 5f;
    [SerializeField] private float height = 2f;

    [Header("Mouse")]
    [SerializeField] private float mouseSensitivity = 3f;

    [Header("Vertical")]
    [SerializeField] private float minPitch = -30f;
    [SerializeField] private float maxPitch = 60f;

    [Header("Camera Collision")]
    [SerializeField] private LayerMask groundLayer;
    [SerializeField] private float collisionRadius = 0.2f;
    [SerializeField] private float collisionOffset = 0.1f;
    [SerializeField] private float minDistance = 0.5f;

    [Header("Camera Shake")]
    [SerializeField] private float shakeDuration = 0.06f;
    [SerializeField] private float shakeStrength = 0.035f;

    private float yaw;
    private float pitch;
    private float shakeTimer;
    private float shakeAmount;

    private void Start()
    {
        if (target == null)
        {
            Debug.LogError("[ThirdPersonCamera] Target chưa được gán!");
            enabled = false;
            return;
        }

        Vector3 angles = transform.eulerAngles;

        yaw = angles.y;
        pitch = angles.x;

        if (pitch > 180f)
        {
            pitch -= 360f;
        }

        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;
    }

    private void LateUpdate()
    {
        if (target == null)
        {
            return;
        }

        HandleMouse();
        UpdateCamera();
    }

    // =========================================================
    // MOUSE
    // =========================================================

    private void HandleMouse()
    {
        float mouseX = Input.GetAxisRaw("Mouse X");
        float mouseY = Input.GetAxisRaw("Mouse Y");

        yaw += mouseX * mouseSensitivity;

        pitch -= mouseY * mouseSensitivity;

        pitch = Mathf.Clamp(
            pitch,
            minPitch,
            maxPitch
        );
    }

    // =========================================================
    // CAMERA
    // =========================================================

    private void UpdateCamera()
    {
        Quaternion rotation = Quaternion.Euler(
            pitch,
            yaw,
            0f
        );

        Vector3 targetPosition =
            target.position +
            Vector3.up * height;

        // Vị trí camera mong muốn
        Vector3 desiredPosition =
            targetPosition -
            rotation * Vector3.forward *
            distance;

        Vector3 direction =
            desiredPosition - targetPosition;

        float targetDistance = direction.magnitude;

        if (targetDistance > 0.01f)
        {
            direction.Normalize();

            // Kiểm tra va chạm giữa player và camera
            if (Physics.SphereCast(
                targetPosition,
                collisionRadius,
                direction,
                out RaycastHit hit,
                targetDistance,
                groundLayer,
                QueryTriggerInteraction.Ignore))
            {
                targetDistance =
                    Mathf.Clamp(
                        hit.distance - collisionOffset,
                        minDistance,
                        distance
                    );

                desiredPosition =
                    targetPosition +
                    direction * targetDistance;
            }
        }

        transform.position = desiredPosition;
        transform.rotation = rotation;

        if (shakeTimer > 0f)
        {
            float shakeProgress = shakeTimer / shakeDuration;
            Vector3 shakeOffset = Random.insideUnitSphere *
                (shakeAmount * shakeProgress);

            transform.position += shakeOffset;
            transform.rotation *= Quaternion.Euler(
                shakeOffset * 20f
            );

            shakeTimer -= Time.deltaTime;
        }
    }

    // =========================================================
    // PUBLIC
    // =========================================================

    public Vector3 GetForward()
    {
        Vector3 forward = transform.forward;

        forward.y = 0f;

        return forward.normalized;
    }

    public Vector3 GetRight()
    {
        Vector3 right = transform.right;

        right.y = 0f;

        return right.normalized;
    }

    public float GetYaw()
    {
        return yaw;
    }

    public void Shake()
    {
        shakeTimer = shakeDuration;
        shakeAmount = shakeStrength;
    }
}