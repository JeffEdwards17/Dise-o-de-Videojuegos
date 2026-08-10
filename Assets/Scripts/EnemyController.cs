using System.Collections;
using UnityEngine;
using UnityEngine.AI;
using UnityEngine.SceneManagement;
using UnityEngine.Serialization;

public enum EnemyState
{
    Patrolling,
    Following
}

[RequireComponent(typeof(NavMeshAgent))]
public class EnemyController : MonoBehaviour
{
    private static readonly int IsWalking = Animator.StringToHash("IsWalking");

    [Header("References")]
    [SerializeField] private Transform player;
    [SerializeField] private Transform[] patrolPoints;

    [Header("Settings")]
    [SerializeField] private float patrolWaitTime = 2f;
    [SerializeField] private float stopAtDistance = 0.5f;
    [SerializeField] private float detectionRange = 5f;
    [SerializeField] private float viewAngle = 90f;
    [FormerlySerializedAs("losePLayerTime")]
    [SerializeField] private float losePlayerTime = 3f;
    [SerializeField] private LayerMask lineOfSightMask = ~0;
    [SerializeField] private float eyeHeight = 1.2f;
    [SerializeField] private float catchDistance = 1.15f;
    [SerializeField] private float restartDelay = 1.25f;

    [Header("Audio")]
    [SerializeField] private AudioSource chaseAudioSource;
    [SerializeField] private AudioClip chaseClip;

    private NavMeshAgent agent;
    private Animator animator;
    private PlayerController playerController;
    private int currentPatrolIndex;
    private bool isWaiting;
    private EnemyState state = EnemyState.Patrolling;
    private float timeSinceLostPlayer;
    private Coroutine waitRoutine;
    private bool isRestarting;

    private void Awake()
    {
        agent = GetComponent<NavMeshAgent>();
        animator = GetComponent<Animator>();

        if (player == null)
        {
            GameObject playerObject = GameObject.FindGameObjectWithTag("Player");
            if (playerObject != null)
                player = playerObject.transform;
        }

        if (player != null)
            playerController = player.GetComponentInParent<PlayerController>();
    }

    private void Start()
    {
        if (!HasValidSetup())
        {
            enabled = false;
            return;
        }

        GoToNextPatrolPoint();
    }

    private void Update()
    {
        if (isRestarting || player == null || agent == null || !agent.enabled || !agent.isOnNavMesh)
            return;

        float distanceToPlayer = Vector3.Distance(player.position, transform.position);
        if (state == EnemyState.Following && !playerController.IsHidden && distanceToPlayer <= catchDistance)
        {
            CatchPlayer();
            return;
        }

        switch (state)
        {
            case EnemyState.Patrolling:
                Patrol();
                if (distanceToPlayer <= detectionRange && CanSeePlayer())
                    BeginFollowing();
                break;

            case EnemyState.Following:
                FollowPlayer();
                if (!CanSeePlayer())
                {
                    timeSinceLostPlayer += Time.deltaTime;
                    if (timeSinceLostPlayer >= losePlayerTime)
                    {
                        state = EnemyState.Patrolling;
                        timeSinceLostPlayer = 0f;
                        agent.isStopped = false;
                        if (chaseAudioSource != null)
                            chaseAudioSource.Stop();
                        GoToClosestPatrolPoint();
                    }
                }
                else
                {
                    timeSinceLostPlayer = 0f;
                }
                break;
        }

        UpdateAnimations();
    }

    private bool HasValidSetup()
    {
        if (player == null || playerController == null || agent == null)
        {
            Debug.LogError("Noc necesita referencias válidas al Player, PlayerController y NavMeshAgent.", this);
            return false;
        }

        if (!agent.enabled || !agent.isOnNavMesh)
        {
            Debug.LogError("Noc no está colocado sobre un NavMesh válido.", this);
            return false;
        }

        if (patrolPoints == null || patrolPoints.Length == 0)
        {
            Debug.LogError("Noc necesita al menos un punto de patrulla.", this);
            return false;
        }

        foreach (Transform point in patrolPoints)
        {
            if (point == null)
            {
                Debug.LogError("La ruta de Noc contiene un punto de patrulla vacío.", this);
                return false;
            }
        }

        return true;
    }

    private void BeginFollowing()
    {
        if (waitRoutine != null)
        {
            StopCoroutine(waitRoutine);
            waitRoutine = null;
        }

        isWaiting = false;
        agent.isStopped = false;
        timeSinceLostPlayer = 0f;
        state = EnemyState.Following;

        if (chaseAudioSource != null && chaseClip != null && !chaseAudioSource.isPlaying)
        {
            chaseAudioSource.clip = chaseClip;
            chaseAudioSource.loop = true;
            chaseAudioSource.Play();
        }

        FollowPlayer();
    }

    private void CatchPlayer()
    {
        isRestarting = true;
        agent.isStopped = true;
        playerController.enabled = false;

        if (chaseAudioSource != null)
            chaseAudioSource.Stop();

        Interactor interactor = player.GetComponent<Interactor>();
        if (interactor != null)
            interactor.enabled = false;

        if (GameMessageUI.Instance != null)
            GameMessageUI.Instance.ShowMessage("Noc te encontró. Regresas al inicio de la cabaña.", restartDelay);

        StartCoroutine(RestartCurrentScene());
    }

    private IEnumerator RestartCurrentScene()
    {
        yield return new WaitForSecondsRealtime(restartDelay);
        Time.timeScale = 1f;
        SceneManager.LoadScene(SceneManager.GetActiveScene().name);
    }

    private void GoToClosestPatrolPoint()
    {
        int closestIndex = 0;
        float closestDistance = float.MaxValue;

        for (int i = 0; i < patrolPoints.Length; i++)
        {
            float distance = Vector3.Distance(transform.position, patrolPoints[i].position);
            if (distance < closestDistance)
            {
                closestDistance = distance;
                closestIndex = i;
            }
        }

        currentPatrolIndex = closestIndex;
        agent.SetDestination(patrolPoints[currentPatrolIndex].position);
    }

    private void FollowPlayer()
    {
        agent.isStopped = false;
        agent.SetDestination(player.position);
    }

    private void Patrol()
    {
        if (isWaiting)
            return;

        if (!agent.pathPending && agent.remainingDistance <= stopAtDistance)
            waitRoutine = StartCoroutine(WaitAtPatrolPoint());
    }

    private IEnumerator WaitAtPatrolPoint()
    {
        isWaiting = true;
        agent.isStopped = true;

        yield return new WaitForSeconds(patrolWaitTime);

        agent.isStopped = false;
        GoToNextPatrolPoint();
        isWaiting = false;
        waitRoutine = null;
    }

    private void GoToNextPatrolPoint()
    {
        if (patrolPoints == null || patrolPoints.Length == 0)
            return;

        agent.SetDestination(patrolPoints[currentPatrolIndex].position);
        currentPatrolIndex = (currentPatrolIndex + 1) % patrolPoints.Length;
    }

    private void UpdateAnimations()
    {
        if (animator == null)
            return;

        bool isMoving = agent.velocity.sqrMagnitude > 0.1f;
        animator.SetBool(IsWalking, isMoving);
    }

    private bool CanSeePlayer()
    {
        if (playerController != null && playerController.IsHidden)
            return false;

        return IsFacingPlayer() && HasClearPathToPlayer();
    }

    private bool IsFacingPlayer()
    {
        Vector3 directionToPlayer = GetPlayerTarget() - GetEyePosition();
        float angle = Vector3.Angle(transform.forward, directionToPlayer.normalized);
        return angle <= viewAngle * 0.5f;
    }

    private bool HasClearPathToPlayer()
    {
        Vector3 origin = GetEyePosition();
        Vector3 target = GetPlayerTarget();
        Vector3 direction = (target - origin).normalized;
        float remainingDistance = Vector3.Distance(origin, target);

        for (int i = 0; i < 8 && remainingDistance > 0.01f; i++)
        {
            if (!Physics.Raycast(origin, direction, out RaycastHit hit, remainingDistance,
                lineOfSightMask, QueryTriggerInteraction.Ignore))
                return true;

            if (BelongsToPlayer(hit.collider))
                return true;

            if (hit.transform.IsChildOf(transform) || IsMinorInteractable(hit.collider))
            {
                float advance = hit.distance + 0.05f;
                origin += direction * advance;
                remainingDistance -= advance;
                continue;
            }

            return false;
        }

        return true;
    }

    private Vector3 GetEyePosition()
    {
        return transform.position + Vector3.up * eyeHeight;
    }

    private Vector3 GetPlayerTarget()
    {
        CharacterController character = player.GetComponent<CharacterController>();
        float targetHeight = character != null ? character.height * 0.5f : 0.9f;
        return player.position + Vector3.up * targetHeight;
    }

    private bool BelongsToPlayer(Collider hit)
    {
        return hit != null && hit.GetComponentInParent<PlayerController>() == playerController;
    }

    private static bool IsMinorInteractable(Collider hit)
    {
        if (hit == null || hit.bounds.size.sqrMagnitude > 2.25f)
            return false;

        MonoBehaviour[] behaviours = hit.GetComponentsInParent<MonoBehaviour>();
        foreach (MonoBehaviour behaviour in behaviours)
        {
            if (behaviour is IInteractable)
                return true;
        }

        return false;
    }
}
