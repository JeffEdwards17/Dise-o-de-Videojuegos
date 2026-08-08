using UnityEngine;

/// <summary>
/// Sonidos de pasos del jugador (HU: sonidos de pasos).
/// Reproduce un clip con ritmo según el estado (caminar, correr, agachado)
/// y ajusta el volumen según la superficie (tierra fuera / madera dentro).
/// </summary>
public class PlayerFootsteps : MonoBehaviour
{
    [Header("Referencias (las conecta el builder)")]
    public PlayerController player;
    public AudioSource source;

    [Header("Clip")]
    public AudioClip stepClip;

    [Header("Ritmo")]
    public float walkInterval = 0.45f;
    public float runInterval = 0.3f;
    public float crouchInterval = 0.6f;

    [Header("Volumen")]
    [Range(0f, 1f)] public float walkVolume = 0.4f;
    [Range(0f, 1f)] public float runVolume = 0.55f;
    [Range(0f, 1f)] public float crouchVolume = 0.18f;
    [Range(0f, 1f)] public float dirtVolumeFactor = 0.7f;

    [Header("Variación")]
    public float pitchMin = 0.9f;
    public float pitchMax = 1.1f;

    [Tooltip("Nombre del objeto del suelo exterior (piso de tierra).")]
    public string dirtGroundName = "NOC_ExteriorGround";

    private CharacterController controller;
    private float nextStepTime;

    private void Awake()
    {
        controller = GetComponent<CharacterController>();

        if (source == null)
            source = GetComponent<AudioSource>();

        if (source != null)
        {
            source.playOnAwake = false;
            source.loop = false;
        }
    }

    private void Update()
    {
        if (player == null || source == null || stepClip == null || controller == null)
            return;

        if (player.IsHidden || !controller.isGrounded)
            return;

        float h = Input.GetAxis("Horizontal");
        float v = Input.GetAxis("Vertical");

        bool moving = Mathf.Abs(h) > 0.1f || Mathf.Abs(v) > 0.1f;
        if (!moving)
        {
            nextStepTime = Time.time + 0.2f;
            return;
        }

        float interval;
        float volume;

        if (player.IsCrouched)
        {
            interval = crouchInterval;
            volume = crouchVolume;
        }
        else if (player.IsSprinting)
        {
            interval = runInterval;
            volume = runVolume;
        }
        else
        {
            interval = walkInterval;
            volume = walkVolume;
        }

        if (Time.time >= nextStepTime)
        {
            nextStepTime = Time.time + interval;

            source.pitch = Random.Range(pitchMin, pitchMax);
            source.volume = volume * SurfaceFactor();
            source.PlayOneShot(stepClip);
        }
    }

    private float SurfaceFactor()
    {
        RaycastHit hit;

        if (Physics.Raycast(transform.position + Vector3.up * 0.15f, Vector3.down, out hit, 5f))
        {
            if (hit.collider != null && hit.collider.name == dirtGroundName)
                return dirtVolumeFactor;
        }

        return 1f;
    }
}
