using UnityEngine;
using UnityEngine.UI;

[RequireComponent(typeof(CharacterController))]
public class PlayerController : MonoBehaviour
{
    [Header("References")]
    public Transform playerCamera;
    public Slider staminaSlider;

    [Header("Movement")]
    public float walkSpeed = 3.2f;
    public float runSpeed = 5.8f;
    public float crouchSpeed = 1.8f;
    public float gravity = -18f;

    [Header("Mouse Look")]
    public float mouseSensitivity = 2f;
    public float maxLookAngle = 85f;

    [Header("Crouch")]
    public KeyCode crouchKey = KeyCode.LeftControl;
    public float standingHeight = 1.8f;
    public float crouchingHeight = 1.05f;
    public float standingCameraY = 1.6f;
    public float crouchingCameraY = 0.95f;

    [Header("Stamina")]
    public float maxStamina = 100f;
    public float staminaDrainPerSecond = 28f;
    public float staminaRegenPerSecond = 18f;
    public float staminaRegenDelay = 0.7f;

    private CharacterController controller;
    private float verticalVelocity;
    private float cameraPitch;
    private float currentStamina;
    private float lastSprintTime;

    private Vector3 positionBeforeHide;
    private Quaternion rotationBeforeHide;

    public bool IsHidden { get; private set; }
    public bool IsSprinting { get; private set; }

    public float StaminaNormalized
    {
        get { return currentStamina / maxStamina; }
    }

    private void Awake()
    {
        controller = GetComponent<CharacterController>();
        currentStamina = maxStamina;

        if (playerCamera == null && Camera.main != null)
            playerCamera = Camera.main.transform;
    }

    private void Start()
    {
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;

        controller.height = standingHeight;
    }

    private void Update()
    {
        if (IsHidden)
            return;

        Look();
        Move();
        UpdateStaminaUI();
    }

    private void Look()
    {
        float mouseX = Input.GetAxis("Mouse X") * mouseSensitivity;
        float mouseY = Input.GetAxis("Mouse Y") * mouseSensitivity;

        transform.Rotate(Vector3.up * mouseX);

        cameraPitch -= mouseY;
        cameraPitch = Mathf.Clamp(cameraPitch, -maxLookAngle, maxLookAngle);
        playerCamera.localRotation = Quaternion.Euler(cameraPitch, 0f, 0f);
    }

    private void Move()
    {
        bool crouching = Input.GetKey(crouchKey);

        controller.height = crouching ? crouchingHeight : standingHeight;

        Vector3 camPos = playerCamera.localPosition;
        camPos.y = crouching ? crouchingCameraY : standingCameraY;
        playerCamera.localPosition = camPos;

        float horizontal = Input.GetAxis("Horizontal");
        float vertical = Input.GetAxis("Vertical");

        Vector3 input = transform.right * horizontal + transform.forward * vertical;
        input = Vector3.ClampMagnitude(input, 1f);

        bool wantsToRun = Input.GetKey(KeyCode.LeftShift);
        bool hasStamina = currentStamina > 1f;
        bool moving = input.magnitude > 0.1f;

        IsSprinting = wantsToRun && hasStamina && moving && !crouching;

        float speed = walkSpeed;

        if (crouching)
            speed = crouchSpeed;
        else if (IsSprinting)
            speed = runSpeed;

        if (IsSprinting)
        {
            currentStamina -= staminaDrainPerSecond * Time.deltaTime;
            currentStamina = Mathf.Max(currentStamina, 0f);
            lastSprintTime = Time.time;
        }
        else
        {
            if (Time.time > lastSprintTime + staminaRegenDelay)
            {
                currentStamina += staminaRegenPerSecond * Time.deltaTime;
                currentStamina = Mathf.Min(currentStamina, maxStamina);
            }
        }

        if (controller.isGrounded && verticalVelocity < 0f)
            verticalVelocity = -2f;

        verticalVelocity += gravity * Time.deltaTime;

        Vector3 velocity = input * speed;
        velocity.y = verticalVelocity;

        controller.Move(velocity * Time.deltaTime);
    }

    private void UpdateStaminaUI()
    {
        if (staminaSlider != null)
            staminaSlider.value = StaminaNormalized;
    }

    public void SetHidden(bool hidden, Transform hidePoint)
    {
        if (hidden)
        {
            positionBeforeHide = transform.position;
            rotationBeforeHide = transform.rotation;

            controller.enabled = false;
            transform.position = hidePoint.position;
            transform.rotation = hidePoint.rotation;
            controller.enabled = true;

            IsHidden = true;
        }
        else
        {
            controller.enabled = false;
            transform.position = positionBeforeHide;
            transform.rotation = rotationBeforeHide;
            controller.enabled = true;

            IsHidden = false;
        }
    }
}