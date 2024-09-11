using UnityEngine;
using UnityEngine.UI;
using System.Collections;

[RequireComponent(typeof(CharacterController))]
public class PlayerController : MonoBehaviour
{
    public float m_WalkSpeed = 6.0f;
    public float m_RunSpeed = 11.0f;
    public bool m_LimitDiagonalSpeed = true;
    public bool m_ToggleRun = false;
    public float m_JumpSpeed = 8.0f;
    public float m_Gravity = 20.0f;
    public float m_FallingThreshold = 10.0f;
    public bool m_SlideWhenOverSlopeLimit = false;
    public bool m_SlideOnTaggedObjects = false;
    public float m_SlideSpeed = 12.0f;
    public bool m_AirControl = false;
    public float m_AntiBumpFactor = .75f;
    public int m_AntiBunnyHopFactor = 1;
    public float touchSens = 2.0f;
    public float lookSmoothness = 0.1f;
    public float moveSmoothness = 0.1f;
    public Transform cam;

    public Joystick moveJoystick; // Assign this in the inspector
    public Joystick lookJoystick; // Assign this in the inspector
    public Button jumpButton; // Assign this in the inspector

    float angle = 0;
    Vector3 m_MoveDirection = Vector3.zero;
    bool m_Grounded = false;
    CharacterController m_Controller;
    Transform m_Transform;
    float m_Speed;
    RaycastHit m_Hit;
    float m_FallStartLevel;
    bool m_Falling;
    float m_SlideLimit;
    float m_RayDistance;
    Vector3 m_ContactPoint;
    bool m_PlayerControl = false;
    int m_JumpTimer;

    public bool controlsEnabled = true;
    private bool jump = false;
    private Vector3 moveVelocity = Vector3.zero;
    private Vector2 lookVelocity = Vector2.zero;
    private Vector3 moveInput = Vector3.zero;
    private Vector2 lookInput = Vector2.zero;
    private Vector3 smoothMove = Vector3.zero;
    private Vector2 smoothLook = Vector2.zero;

    void Start()
    {
        m_Transform = GetComponent<Transform>();
        m_Controller = GetComponent<CharacterController>();

        m_Speed = m_WalkSpeed;
        m_RayDistance = m_Controller.height * .5f + m_Controller.radius;
        m_SlideLimit = m_Controller.slopeLimit - .1f;
        m_JumpTimer = m_AntiBunnyHopFactor;

        if (jumpButton != null)
        {
            jumpButton.onClick.AddListener(OnJumpButtonPressed);
        }
    }

    void Update()
    {
        // Ensure the cursor is visible on mobile platforms
        Cursor.visible = true;
        Cursor.lockState = CursorLockMode.None;

        HandleLook();

        // Re-enable controls when unpausing
        if (!PauseMenu.pauseMenu.paused && !GetComponent<PlayerIO>().inventory.activeSelf)
        {
            controlsEnabled = true;
        }
    }

    void HandleLook()
    {
        if (!cam)
        {
            print("No camera assigned to PlayerController!");
            return;
        }

        // Get look input from joystick
        lookInput = new Vector2(lookJoystick.Horizontal, lookJoystick.Vertical) * touchSens;

        lookInput.y = PauseMenu.pauseMenu.invertMouse ? -lookInput.y : lookInput.y;

        // Smooth look
        smoothLook = Vector2.SmoothDamp(smoothLook, lookInput, ref lookVelocity, lookSmoothness);

        // Apply look
        transform.Rotate(Vector3.up, smoothLook.x);
        angle = Mathf.Clamp(angle - smoothLook.y, -90, 90);
        cam.localEulerAngles = new Vector3(angle, 0, 0);
    }

    void FixedUpdate()
    {
        if (PauseMenu.pauseMenu.paused || GetComponent<PlayerIO>().inventory.activeSelf || !controlsEnabled) return;

        HandleMovement();
    }

    void HandleMovement()
    {
        float inputX = moveJoystick.Horizontal;
        float inputY = moveJoystick.Vertical;

        float inputModifyFactor = (inputX != 0.0f && inputY != 0.0f && m_LimitDiagonalSpeed) ? .7071f : 1.0f;

        if (m_Grounded)
        {
            bool sliding = false;
            if (Physics.Raycast(m_Transform.position, -Vector3.up, out m_Hit, m_RayDistance))
            {
                if (Vector3.Angle(m_Hit.normal, Vector3.up) > m_SlideLimit)
                {
                    sliding = true;
                }
            }
            else
            {
                Physics.Raycast(m_ContactPoint + Vector3.up, -Vector3.up, out m_Hit);
                if (Vector3.Angle(m_Hit.normal, Vector3.up) > m_SlideLimit)
                {
                    sliding = true;
                }
            }
            if (m_Falling)
            {
                m_Falling = false;
                if (m_Transform.position.y < m_FallStartLevel - m_FallingThreshold)
                {
                    OnFell(m_FallStartLevel - m_Transform.position.y);
                }
            }
            m_Speed = m_WalkSpeed; // Always use walk speed for mobile

            if ((sliding && m_SlideWhenOverSlopeLimit) || (m_SlideOnTaggedObjects && m_Hit.collider.tag == "Slide"))
            {
                Vector3 hitNormal = m_Hit.normal;
                m_MoveDirection = new Vector3(hitNormal.x, -hitNormal.y, hitNormal.z);
                Vector3.OrthoNormalize(ref hitNormal, ref m_MoveDirection);
                m_MoveDirection *= m_SlideSpeed;
                m_PlayerControl = false;
            }
            else
            {
                moveInput = new Vector3(inputX * inputModifyFactor, -m_AntiBumpFactor, inputY * inputModifyFactor);
                moveInput = m_Transform.TransformDirection(moveInput) * m_Speed;

                // Smooth movement
                smoothMove = Vector3.SmoothDamp(smoothMove, moveInput, ref moveVelocity, moveSmoothness);

                m_MoveDirection = smoothMove;
                m_PlayerControl = true;
            }
            if (!jump)
            {
                m_JumpTimer++;
            }
            else if (m_JumpTimer >= m_AntiBunnyHopFactor && m_Grounded) // Ensure jumping is grounded
            {
                m_MoveDirection.y = m_JumpSpeed;
                m_JumpTimer = 0;
                jump = false; // Reset jump flag
            }
        }
        else
        {
            if (!m_Falling)
            {
                m_Falling = true;
                m_FallStartLevel = m_Transform.position.y;
            }
            if (m_AirControl && m_PlayerControl)
            {
                moveInput = new Vector3(inputX * m_Speed * inputModifyFactor, m_MoveDirection.y, inputY * m_Speed * inputModifyFactor);
                moveInput = m_Transform.TransformDirection(moveInput);

                // Smooth movement
                smoothMove = Vector3.SmoothDamp(smoothMove, moveInput, ref moveVelocity, moveSmoothness);

                m_MoveDirection.x = smoothMove.x;
                m_MoveDirection.z = smoothMove.z;
            }
        }

        m_MoveDirection.y -= m_Gravity * Time.deltaTime;
        m_Grounded = (m_Controller.Move(m_MoveDirection * Time.deltaTime) & CollisionFlags.Below) != 0;
    }

    // Place this inside PlayerController.cs
    void OnJumpButtonPressed()
    {
        if (m_Grounded && !jump) // Only jump if grounded and jump flag is not already set
        {
            jump = true;
            StartCoroutine(ResetJumpFlag()); // Reset jump flag with debounce
        }
    }

    // Debounce logic for jump input to prevent rapid jumps
    private IEnumerator ResetJumpFlag()
    {
        yield return new WaitForSeconds(0.2f); // Adjust the debounce time as needed
        jump = false; // Reset jump flag after cooldown
    }


    void OnControllerColliderHit(ControllerColliderHit hit)
    {
        m_ContactPoint = hit.point;
    }

    void OnFell(float fallDistance)
    {
        if (fallDistance >= 4 && fallDistance < 12)
            SoundManager.PlayAudio("fallsmall", 0.25f, Random.Range(0.9f, 1.1f));
        else if (fallDistance >= 12)
        {
            SoundManager.PlayAudio("fallbig", 0.25f, Random.Range(0.9f, 1.1f));
            World.currentWorld.SpawnLandParticles();
        }
    }
}
