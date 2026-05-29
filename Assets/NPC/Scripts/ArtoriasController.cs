using UnityEngine;
using UnityEngine.AI;
using System.Collections;

[RequireComponent(typeof(NavMeshAgent))]
public class ArtoriasController : MonoBehaviour, IDamageable
{
    [Header("Stats")]
    public float maxHealth      = 300f;
    public float attackDamage   = 28f;
    public float attackCooldown = 1.5f;
    public float moveSpeed      = 1.8f;

    [Header("Detection")]
    public float detectionRange = 6f;
    public float attackRange    = 1.2f;

    [Header("Patrol")]
    public float patrolRadius   = 8f;
    public float patrolWaitTime = 3f;

    [Header("Rewards")]
    public int coinReward = 50;

    private static readonly int HashIsWalking  = Animator.StringToHash("IsWalking");
    private static readonly int HashIsRunning  = Animator.StringToHash("IsRunning");
    private static readonly int HashAttack     = Animator.StringToHash("Attack");
    private static readonly int HashAttack2    = Animator.StringToHash("Attack2");
    private static readonly int HashAttack3    = Animator.StringToHash("Attack3");
    private static readonly int HashTakeDamage = Animator.StringToHash("TakeDamage");
    private static readonly int HashDie        = Animator.StringToHash("Die");

    private enum State { Idle, Patrolling, Chasing, Attacking, Dead }
    private State      _state = State.Idle;
    private Animator   _animator;
    private NavMeshAgent _agent;
    private Transform  _player;
    private float      _health, _lastAttackTime, _patrolWaitTimer, _attackingExitTimer;
    private int        _attackStep;
    private Vector3    _patrolTarget, _spawnPosition;
    private bool       _inCombat;

    private Transform _modelRoot;

    private void Awake()
    {
        _agent    = GetComponent<NavMeshAgent>();
        _animator = GetComponentInChildren<Animator>();
        _health   = maxHealth;
        _spawnPosition = transform.position;
        _modelRoot = transform.childCount > 0 ? transform.GetChild(0) : null;
        _agent.speed = moveSpeed;
        _agent.stoppingDistance = attackRange * 0.5f;
        _patrolWaitTimer = patrolWaitTime;
        if (_animator != null) _animator.applyRootMotion = false;
        int el = LayerMask.NameToLayer("Enemy"); if (el >= 0) gameObject.layer = el;
        if (!_agent.isOnNavMesh) { NavMeshHit h; if (NavMesh.SamplePosition(transform.position, out h, 20f, NavMesh.AllAreas)) _agent.Warp(h.position); }
        PickPatrolTarget();
    }

    private void Update()
    {
        if (_state == State.Dead) return;
        if (_state != State.Chasing && _state != State.Attacking) TryDetect();
        switch (_state)
        {
            case State.Idle:       UpdateIdle();    break;
            case State.Patrolling: UpdatePatrol();  break;
            case State.Chasing:    UpdateChase();   break;
            case State.Attacking:  UpdateAttack();  break;
        }
    }

    private void TryDetect()
    {
        foreach (var col in Physics.OverlapSphere(transform.position, detectionRange))
        {
            if (!col.CompareTag("Player")) continue;
            _player = col.transform;
            if (!_inCombat) { _inCombat = true; GameMusicManager.Instance?.EnterCombat(); }
            _state = State.Chasing; break;
        }
    }

    private void UpdateIdle()
    {
        _animator?.SetBool(HashIsWalking, false); _animator?.SetBool(HashIsRunning, false);
        _patrolWaitTimer -= Time.deltaTime;
        if (_patrolWaitTimer <= 0f) { PickPatrolTarget(); _state = State.Patrolling; }
    }

    private void UpdatePatrol()
    {
        if (!_agent.isOnNavMesh) { _state = State.Idle; return; }
        _agent.isStopped = false; _agent.speed = moveSpeed;
        _animator?.SetBool(HashIsWalking, true); _animator?.SetBool(HashIsRunning, false);
        if (!_agent.hasPath || _agent.pathStatus == NavMeshPathStatus.PathInvalid) _agent.SetDestination(_patrolTarget);
        bool arrived = !_agent.pathPending && _agent.remainingDistance <= _agent.stoppingDistance + 0.3f && _agent.velocity.sqrMagnitude < 0.05f;
        if (arrived) { _patrolWaitTimer = patrolWaitTime; _state = State.Idle; }
    }

    private void UpdateChase()
    {
        if (_player == null) { _state = State.Idle; return; }
        float dist = Vector3.Distance(transform.position, _player.position);
        if (dist <= attackRange && _player != null && _player.CompareTag("Player"))
        {
            if (_agent.isOnNavMesh) _agent.isStopped = true;
            _state = State.Attacking; _attackingExitTimer = 0f;
            _animator?.SetBool(HashIsWalking, false); _animator?.SetBool(HashIsRunning, false);
        }
        else
        {
            if (_agent.isOnNavMesh) { _agent.isStopped = false; _agent.speed = moveSpeed * 1.8f; _agent.SetDestination(_player.position); }
            _animator?.SetBool(HashIsWalking, false); _animator?.SetBool(HashIsRunning, true);
        }
        if (dist > detectionRange * 2f) { if (_inCombat) { _inCombat = false; GameMusicManager.Instance?.ExitCombat(); } _animator?.SetBool(HashIsRunning, false); _state = State.Patrolling; _player = null; }
    }

    private void UpdateAttack()
    {
        if (_player == null) { _state = State.Idle; return; }
        float dist = Vector3.Distance(transform.position, _player.position);
        Vector3 dir = _player.position - transform.position; dir.y = 0f;
        if (dir != Vector3.zero) transform.rotation = Quaternion.Slerp(transform.rotation, Quaternion.LookRotation(dir), 10f * Time.deltaTime);
        _attackingExitTimer += Time.deltaTime;
        if (_attackingExitTimer >= 0.4f && dist > attackRange * 1.5f) { _state = State.Chasing; if (_agent.isOnNavMesh) _agent.isStopped = false; return; }
        if (Time.time >= _lastAttackTime + attackCooldown && _player != null && _player.CompareTag("Player"))
        {
            _lastAttackTime = Time.time;
            var hash = (_attackStep % 3) == 0 ? HashAttack : (_attackStep % 3) == 1 ? HashAttack2 : HashAttack3;
            _animator?.SetTrigger(hash); _attackStep++;
            var ph = _player.GetComponentInParent<PlayerHealth>() ?? _player.GetComponentInChildren<PlayerHealth>();
            ph?.TakeDamage(attackDamage);
        }
    }

    public void TakeDamage(float amount)
    {
        if (_state == State.Dead) return;
        _health -= amount;
        if (_player == null) { _player = GameObject.FindGameObjectWithTag("Player")?.transform; if (!_inCombat) { _inCombat = true; GameMusicManager.Instance?.EnterCombat(); } _state = State.Chasing; }
        _animator?.SetTrigger(HashTakeDamage);
        if (_health <= 0f) Die();
    }

    private void Die()
    {
        _state = State.Dead;
        if (_inCombat) { _inCombat = false; GameMusicManager.Instance?.ExitCombat(); }
        if (_agent.isOnNavMesh) _agent.isStopped = true; _agent.enabled = false;
        _animator?.SetTrigger(HashDie); PlayerInventory.Instance?.AddCoins(coinReward);
        StartCoroutine(DeathRoutine());
    }

    private void LateUpdate()
    {
        if (_modelRoot != null)
        {
            var lp = _modelRoot.localPosition;
            lp.y = 0f;
            _modelRoot.localPosition = lp;
        }
    }

    private IEnumerator DeathRoutine() { yield return new WaitForSeconds(4f); Destroy(gameObject); }

    private void PickPatrolTarget()
    {
        for (int i = 1; i <= 8; i++)
        {
            Vector2 c = Random.insideUnitCircle.normalized * patrolRadius;
            Vector3 candidate = _spawnPosition + new Vector3(c.x, 0, c.y);
            if (NavMesh.SamplePosition(candidate, out NavMeshHit hit, patrolRadius * i * 0.5f, NavMesh.AllAreas))
                if (Vector3.Distance(transform.position, hit.position) > 1.5f) { _patrolTarget = hit.position; return; }
        }
        _patrolTarget = transform.position + transform.forward * patrolRadius * 0.5f;
    }

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.cyan; Gizmos.DrawWireSphere(transform.position, detectionRange);
        Gizmos.color = Color.red; Gizmos.DrawWireSphere(transform.position, attackRange);
    }
}







