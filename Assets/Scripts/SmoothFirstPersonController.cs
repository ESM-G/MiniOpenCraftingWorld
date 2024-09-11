using UnityEngine;

public class SmoothFirstPersonController : MonoBehaviour
{
    public float moveSpeed = 5.0f;
    public float lookSpeed = 2.0f;
    public float smoothTime = 0.1f;

    private Vector3 moveVelocity = Vector3.zero;
    private Vector2 lookVelocity = Vector2.zero;
    private Vector3 moveInput = Vector3.zero;
    private Vector2 lookInput = Vector2.zero;
    private Vector3 smoothMove = Vector3.zero;
    private Vector2 smoothLook = Vector2.zero;

    void Update()
    {
        HandleMovement();
        HandleLook();
    }

    void HandleMovement()
    {
        // Get movement input
        moveInput = new Vector3(Input.GetAxis("Horizontal"), 0, Input.GetAxis("Vertical"));
        moveInput = transform.TransformDirection(moveInput);
        moveInput *= moveSpeed;

        // Smooth movement
        smoothMove = Vector3.SmoothDamp(smoothMove, moveInput, ref moveVelocity, smoothTime);

        // Apply movement
        transform.position += smoothMove * Time.deltaTime;
    }

    void HandleLook()
    {
        // Get look input
        lookInput = new Vector2(Input.GetAxis("Mouse X"), Input.GetAxis("Mouse Y"));
        lookInput *= lookSpeed;

        // Smooth look
        smoothLook = Vector2.SmoothDamp(smoothLook, lookInput, ref lookVelocity, smoothTime);

        // Apply look
        transform.Rotate(Vector3.up, smoothLook.x);
        Camera.main.transform.Rotate(Vector3.left, -smoothLook.y);
    }
}
