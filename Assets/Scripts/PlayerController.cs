using UnityEngine;
using UnityEngine.UI;

[RequireComponent(typeof(CharacterController))]
public class PlayerController : MonoBehaviour
{
    [Header("References")]
    public Transform playerCamera;
    public Slider staminaSlider;

    [Header("Movement")]
    public float walkSpeed = 4f;
    public float runSpeed = 7f;
    public float crouchSpeed = 2.2f;
    public float gravity = -18f;

    [Header("Mouse Look")]
    public float mouseSensitivity = 2f;
    public float maxLookAngle = 85f;

    [Header("Debug")]
    [SerializeField] private bool debugMovement = false;

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
    private float cameraYBeforeHide;

    private bool exhausted;

    public bool IsHidden { get; private set; }
    public bool IsSprinting { get; private set; }
    public bool IsCrouched { get; private set; }

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
        controller.height = standingHeight;

        // Si la intro está activa, ella desactiva este componente en su Awake
        // (antes de este Start) y es quien bloquea el cursor. En cualquier otro
        // caso, bloqueamos aquí para que WASD y el mouse funcionen siempre.
        if (!IsHidden)
        {
            Cursor.lockState = CursorLockMode.Locked;
            Cursor.visible = false;
        }

        if (debugMovement)
            LogInitial();
    }

    private void Update()
    {
        if (IsHidden)
        {
            Look();
            return;
        }

        Look();
        Move();
        UpdateStaminaUI();
    }

    private void Look()
    {
        if (Cursor.visible)
            return;

        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;

        float mouseX = Input.GetAxis("Mouse X") * mouseSensitivity;
        float mouseY = Input.GetAxis("Mouse Y") * mouseSensitivity;

        // FPS convencional: mouse arriba -> mirar arriba (pitch negativo).
        float pitch = cameraPitch - mouseY;
        pitch = Mathf.Clamp(pitch, -maxLookAngle, maxLookAngle);
        cameraPitch = pitch;

        transform.Rotate(0f, mouseX, 0f);
        playerCamera.localRotation = Quaternion.Euler(pitch, 0f, 0f);
    }

    private void Move()
    {
        if (Cursor.visible)
            return;

        float vertical = Input.GetAxis("Vertical");
        float horizontal = Input.GetAxis("Horizontal");

        bool moving = Mathf.Abs(vertical) > 0.1f || Mathf.Abs(horizontal) > 0.1f;
        bool crouching = Input.GetKey(KeyCode.LeftControl);
        bool wasCrouched = IsCrouched;
        bool wasExhausted = exhausted;
        IsCrouched = crouching;

        bool wantsSprint = Input.GetKey(KeyCode.LeftShift) && moving && !crouching;

        // --- Stamina: correr consume, soltar recupera tras un breve retardo ---
        bool wasSprinting = IsSprinting;
        if (wantsSprint && currentStamina > 0f)
        {
            currentStamina -= staminaDrainPerSecond * Time.deltaTime;
            currentStamina = Mathf.Max(0f, currentStamina);
            lastSprintTime = Time.time;
            IsSprinting = true;

            if (currentStamina <= 0f)
                exhausted = true;
        }
        else
        {
            IsSprinting = false;

            if (currentStamina < maxStamina && Time.time > lastSprintTime + staminaRegenDelay)
            {
                currentStamina += staminaRegenPerSecond * Time.deltaTime;
                currentStamina = Mathf.Min(currentStamina, maxStamina);

                // El agotamiento se libera al recuperar al menos un 20% de reserva.
                if (exhausted && currentStamina >= maxStamina * 0.2f)
                    exhausted = false;
            }
        }

        // Tras agotarse solo se vuelve a correr con al menos un 20% de reserva.
        bool sprinting = wantsSprint && currentStamina > 0f && !exhausted;

        if (crouching)
        {
            controller.height = crouchingHeight;
            controller.center = new Vector3(0f, crouchingHeight * 0.5f, 0f);
        }
        else if (CanStand())
        {
            controller.height = standingHeight;
            controller.center = new Vector3(0f, standingHeight * 0.5f, 0f);
        }

        float speed = walkSpeed;
        if (sprinting)
            speed = runSpeed;
        else if (crouching)
            speed = crouchSpeed;

        // Penalización temporal por agotamiento: solo mientras se intenta
        // correr con la reserva vacía (desaparece al recuperar >= 20%).
        if (wantsSprint && currentStamina <= 0f)
            speed = walkSpeed * 0.6f;

        // Dirección de entrada normalizada ANTES de aplicar la velocidad.
        // (Normalizar después habría dejado la magnitud en 1 sin importar
        //  walk/run/crouch, que es el bug de movimiento lento.)
        Vector3 move = transform.right * horizontal + transform.forward * vertical;
        if (move.magnitude > 1f)
            move = move.normalized;
        move *= speed;

        verticalVelocity += gravity * Time.deltaTime;

        Vector3 velocity = move;
        velocity.y = verticalVelocity;

        controller.Move(velocity * Time.deltaTime);

        if (controller.isGrounded && verticalVelocity < 0f)
            verticalVelocity = -2f;

        // --- Diagnóstico: log SOLO cuando cambia un estado ---
        string transition = null;
        if (IsSprinting != wasSprinting)
            transition = IsSprinting ? "Sprint iniciado" : "Sprint finalizado";
        else if (IsCrouched != wasCrouched)
            transition = IsCrouched ? "Crouch iniciado" : "Crouch finalizado";
        else if (exhausted != wasExhausted)
            transition = exhausted ? "Stamina agotada" : "Stamina recuperada";

        if (transition != null)
            LogState(transition, speed);
    }

    private void LogInitial()
    {
        if (!debugMovement)
            return;

        Debug.Log("[PlayerMovement] INITIAL | IsCrouched=" + IsCrouched +
            " | CharacterHeight=" + controller.height.ToString("0.00") +
            " | Walk=" + walkSpeed + " | Run=" + runSpeed + " | Crouch=" + crouchSpeed +
            " | Stamina=" + currentStamina.ToString("0.0") +
            " | Exhausted=" + exhausted);
    }

    private void LogState(string state, float finalSpeed)
    {
        if (!debugMovement)
            return;

        Debug.Log("[PlayerMovement] Estado: " + state +
            " | crouched=" + IsCrouched +
            " | sprinting=" + IsSprinting +
            " | exhausted=" + exhausted +
            " | stamina=" + currentStamina.ToString("0.0") +
            " | walk=" + walkSpeed +
            " | run=" + runSpeed +
            " | crouch=" + crouchSpeed +
            " | finalSpeed=" + finalSpeed.ToString("0.00"));
    }

    private void UpdateStaminaUI()
    {
        if (staminaSlider != null)
            staminaSlider.value = StaminaNormalized;
    }

    private bool CanStand()
    {
        if (controller == null)
            return true;

        float grow = standingHeight - controller.height;
        if (grow <= 0.01f)
            return true;

        // El cast parte por encima del borde superior del capsule del propio
        // CharacterController para no detectarlo a sí mismo, y por seguridad
        // se ignora un hipotético hit sobre él (era la causa de quedarse
        // agachado permanentemente tras agacharse).
        Vector3 origin = transform.position + Vector3.up * (controller.height + controller.radius + 0.05f);
        RaycastHit hit;
        if (!Physics.SphereCast(origin, controller.radius, Vector3.up, out hit,
            grow + 0.15f, ~0, QueryTriggerInteraction.Ignore))
            return true;

        return hit.collider == null || hit.collider == controller;
    }

    public void SetHidden(bool hidden, Transform hidePoint, float cameraY)
    {
        if (hidden)
        {
            positionBeforeHide = transform.position;
            rotationBeforeHide = transform.rotation;
            cameraYBeforeHide = playerCamera != null
                ? playerCamera.localPosition.y
                : standingCameraY;

            controller.enabled = false;
            transform.position = hidePoint.position;
            transform.rotation = hidePoint.rotation;

            if (playerCamera != null)
                playerCamera.localPosition = new Vector3(0f, cameraY, 0f);

            IsHidden = true;
        }
        else
        {
            ExitHide();
        }
    }

    public void ExitHide(Transform exitPoint = null)
    {
        if (exitPoint != null)
        {
            transform.position = exitPoint.position;
            transform.rotation = exitPoint.rotation;
        }
        else
        {
            transform.position = positionBeforeHide;
            transform.rotation = rotationBeforeHide;
        }

        controller.enabled = true;

        if (playerCamera != null)
            playerCamera.localPosition = new Vector3(0f, cameraYBeforeHide, 0f);

        IsHidden = false;
    }
}
