using UnityEngine;

public class ThirdPersonAnimation : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private Animator animator;

    [Header("Animator Parameter")]
    [SerializeField] private string speedParameter = "Speed";

    [Header("Animation")]
    [SerializeField] private float smoothTime = 0.1f;

    private float currentSpeed;

    private void Awake()
    {
        if (animator == null)
        {
            animator =
                GetComponentInChildren<Animator>();
        }
    }

    private void Update()
    {
        UpdateAnimation();
    }

    private void UpdateAnimation()
    {
        float horizontal =
            Input.GetAxisRaw("Horizontal");

        float vertical =
            Input.GetAxisRaw("Vertical");

        Vector2 input =
            new Vector2(
                horizontal,
                vertical
            );

        float inputMagnitude =
            Mathf.Clamp01(input.magnitude);

        bool isMoving =
            inputMagnitude > 0.01f;

        bool isRunning =
            Input.GetKey(KeyCode.LeftShift);

        float targetSpeed = 0f;

        // IDLE
        if (!isMoving)
        {
            targetSpeed = 0f;
        }

        // RUN
        else if (isRunning)
        {
            targetSpeed = 1f;
        }

        // WALK
        else
        {
            targetSpeed = 0.5f;
        }

        // Smooth transition
        currentSpeed =
            Mathf.Lerp(
                currentSpeed,
                targetSpeed,
                Time.deltaTime / smoothTime
            );

        if (animator != null)
        {
            animator.SetFloat(
                speedParameter,
                currentSpeed
            );
        }
    }
}