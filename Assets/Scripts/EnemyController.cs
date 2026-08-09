
using System.Collections;
using System.Runtime.CompilerServices;
using UnityEngine;
using UnityEngine.AI;

public enum EnemyState
{
    Patrolling,
    Following
}

public class EnemyController : MonoBehaviour
{
    private static readonly int IsWalking = Animator.StringToHash("IsWalking");


    [Header("Refreences")]
    [SerializeField] private Transform player;
    [SerializeField] private Transform[] patrolPoints;

    [Header("Settings")]
    [SerializeField] private float patrolWaitTime = 2f;
    [SerializeField] private float stopAtDistance = 0.5f;
    [SerializeField] private float detectionRange = 5f;
    [SerializeField] private float viewAngle = 90f;
    [SerializeField] private float losePLayerTime = 3f;

    private NavMeshAgent _agent;
    private Animator _animator;
    private int _currentPatrolIndex;
    private bool _isWaiting;
    private EnemyState _state = EnemyState.Patrolling;
    private float _timeSinceLostPlayer;

    private void Awake()
    {
        _agent = GetComponent<NavMeshAgent>();
        _animator = GetComponent<Animator>();
    }

    private void Start()
    {
        GoToNextPatrolPoint();
    }
    private void Update()
    {
        var distanceToPlayer = Vector3.Distance(player.position, transform.position);

        switch (_state)
        {
            case EnemyState.Patrolling:
                Patrol();
                if (distanceToPlayer <= detectionRange && CanSeePLayer())
                {
                    _state = EnemyState.Following;
                }
                break;

            case EnemyState.Following:
                FollowPlayer();
                if (!CanSeePLayer())
                {
                    _timeSinceLostPlayer += Time.deltaTime;
                    if (_timeSinceLostPlayer >= losePLayerTime)
                    {
                        _state = EnemyState.Patrolling;
                        GoToClosestPatrolPoint();
                    }
                } else
                {
                    _timeSinceLostPlayer = 0f;
                }
                break;
        }
        
        UpdateAnimations();
    }

    private void GoToClosestPatrolPoint()
    {
        if (patrolPoints.Length == 0) return;
        var closestIndex = 0;
        var closestDistance = float.MaxValue;

        for (var i = 0; i < patrolPoints.Length; i++)
        {
            var distance = Vector3.Distance(transform.position, patrolPoints[i].position);
            if (distance < closestDistance)
            {
                closestDistance = distance;
                closestIndex = i;
            }
        }

        _currentPatrolIndex = closestIndex;
        _agent.SetDestination(patrolPoints[_currentPatrolIndex].position);
    }
    private void FollowPlayer()
    {
        _agent.SetDestination(player.position);
    }

    private void Patrol()
    {
        if (_isWaiting) return;

        if (!_agent.pathPending && _agent.remainingDistance <= stopAtDistance)
        { 
        StartCoroutine(WaitAtPatrolPoint());
        }
    }
    private IEnumerator WaitAtPatrolPoint()
    {
        _isWaiting = true;
        _agent.isStopped = true;

        yield return new WaitForSeconds(patrolWaitTime);

        _agent.isStopped = false;
        GoToNextPatrolPoint();
        _isWaiting = false;
    }

    private void GoToNextPatrolPoint()
    {
        if (patrolPoints.Length == 0) return;

        _agent.SetDestination(patrolPoints[_currentPatrolIndex].position);
        _currentPatrolIndex = (_currentPatrolIndex + 1) % patrolPoints.Length;
    }

    private void UpdateAnimations()
    {
        var isMoving = _agent.velocity.sqrMagnitude > 0.1f;
        _animator.SetBool("IsWalking", isMoving);
    }

    private bool CanSeePLayer()
    {
        return IsFacingPlayer() && HasClearPathToPlayer();
    }

    private bool IsFacingPlayer()
    {
        var dirToPlayer = (player.position - transform.position).normalized;
        var angle = Vector3.Angle(transform.forward, dirToPlayer);
        return angle <= viewAngle / 2f;
    }

    private bool HasClearPathToPlayer()
    {
        var dirToPlayer = player.position - transform.position;
        if (Physics.Raycast(transform.position, dirToPlayer.normalized, out RaycastHit hit, dirToPlayer.magnitude))
        {
            return hit.transform == player;
        }

        return true;
    }
}
