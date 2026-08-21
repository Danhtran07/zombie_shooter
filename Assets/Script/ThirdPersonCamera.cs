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

    private float yaw;
    private float pitch;

    private void Start()
    {
        if (target == null)
        {
            Debug.LogError(
                "[ThirdPersonCamera] Target chưa được gán!"
            );

            enabled = false;
            return;
        }

        // Lấy rotation ban đầu của camera
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
        float mouseX =
            Input.GetAxisRaw("Mouse X");

        float mouseY =
            Input.GetAxisRaw("Mouse Y");

        yaw +=
            mouseX *
            mouseSensitivity;

        pitch -=
            mouseY *
            mouseSensitivity;

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
        Quaternion rotation =
            Quaternion.Euler(
                pitch,
                yaw,
                0f
            );

        Vector3 targetPosition =
            target.position +
            Vector3.up *
            height;

        Vector3 cameraPosition =
            targetPosition -
            rotation *
            Vector3.forward *
            distance;

        transform.position =
            cameraPosition;

        transform.rotation =
            rotation;
    }

    // =========================================================
    // PUBLIC
    // =========================================================

    public Vector3 GetForward()
    {
        Vector3 forward =
            transform.forward;

        forward.y = 0f;

        return forward.normalized;
    }

    public Vector3 GetRight()
    {
        Vector3 right =
            transform.right;

        right.y = 0f;

        return right.normalized;
    }

    public float GetYaw()
    {
        return yaw;
    }
}