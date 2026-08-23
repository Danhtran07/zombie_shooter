using UnityEngine;

public sealed class ThirdPersonInput
{
    public Vector2 ReadMovement()
    {
        Vector2 input = new Vector2(
            Input.GetAxisRaw("Horizontal"),
            Input.GetAxisRaw("Vertical")
        );

        return Vector2.ClampMagnitude(input, 1f);
    }

    public bool IsJumpPressed()
    {
        return Input.GetKeyDown(KeyCode.Space);
    }
}