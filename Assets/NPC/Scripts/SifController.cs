using UnityEngine;
using UnityEngine.AI;
using System.Collections;

[RequireComponent(typeof(NavMeshAgent))]
public class SifController : MonoBehaviour, IDamageable
{
    [Header("Stats")]
    public float maxHealth      = 200f;
    public float attackDamage   = 22f;
    public float attackCooldown = 1.8f;
    public float moveSpeed      = 4f;

    [Header("Detection")]
    public float detectionRange = 6f;
    public float attackRange    = 1.2f;

    [Header("Patrol")]
    public float patrolRadius   = 7f;
    public float patrolWaitTime = 3f;

    [Header("Rewards")]
    public int coinReward = 30;

    private static readonly int HashIsWalking  = Animator.StringToHash("IsWalking");
    private static readonly int HashIsRunning  = Animator.StringToHash("IsRunning");
    private static readonly int HashAttack     = Animator.StringToHash("Attack");
    private static readonly int HashAttack2    = Animator.StringToHash("Attack2");
    private static readonly int HashTakeDamage = Animator.StringToHash("TakeDamage");
    private static readonly int HashDie        = Animator.StringToHash("Die");

    private enum State { Idle, Patrolling, Chasing, Attacking, Dead }
    private State _state = State.Idle;

    private Animator     _animator;
    private NavMeshAgent _agent;
    private Transform    _player;
    private float        _health;
    private float        _lastAttackTime;
    private float        _patrolWaitTimer;
    private float        _attackingExitTimer;
    private int          _attackStep;
    private Vector3      _patrolTarget;
    private Vector3      _spawnPosition;
    private bool _inCombat;
    private PlayerHealth _playerHealth;

    private void Awake()
    {
        _agent         = GetComponent<NavMeshAgent>();
        _animator      = GetComponentInChildren<Animator>();
        _health        = maxHealth;
        _spawnPosition = transform.position;
        _agent.speed   = moveSpeed;
        _agent.stoppingDistance = attackRange * 0.85f;
        _patrolWaitTimer = patrolWaitTime;

        if (_animator != null) _animator.applyRootMotion = false;

        int enemyLayer = LayerMask.NameToLayer("Enemy");
        if (enemyLayer >= 0) gameObject.layer = enemyLayer;

        if (!_agent.isOnNavMesh)
            if (NavMesh.SamplePosition(transform.position, out NavMeshHit hit, 5f, NavMesh.AllAreas))
                _agent.Warp(hit.position);

        PickPatrolTarget();
    }

    private void Update()
    {
        if (_state == State.Dead) return;

        if (_state != State.Chasing && _state != State.Attacking)
            TryDetect();

        // Se o player morreu, voltar a patrulhar
        if (_player != null)
        {
            if (_playerHealth == null) _playerHealth = _player.GetComponentInParent<PlayerHealth>() ?? _player.GetComponentInChildren<PlayerHealth>();
            if (_playerHealth != null && _playerHealth.IsDead)
            {
                _player = null; _playerHealth = null;
                if (_inCombat) { _inCombat = false; GameMusicManager.Instance?.ExitCombat(); }
                _animator?.SetBool(HashIsWalking, false); _animator?.SetBool(HashIsRunning, false);
                _state = State.Patrolling;
            }
        }

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
            var ph = col.GetComponentInParent<PlayerHealth>() ?? col.GetComponentInChildren<PlayerHealth>();
            if (ph != null && ph.IsDead) continue;
            _player = col.transform;
            if (!_inCombat) { _inCombat = true; GameMusicManager.Instance?.EnterCombat(); }
            _state = State.Chasing;
            break;
        }
    }

    private void UpdateIdle()
    {
        _animator?.SetBool(HashIsWalking, false);
        _patrolWaitTimer -= Time.deltaTime;
        if (_patrolWaitTimer <= 0f) { PickPatrolTarget(); _state = State.Patrolling; }
    }

    private void UpdatePatrol()
    {
        _agent.isStopped = false;
        _animator?.SetBool(HashIsWalking, true);

        if (!_agent.hasPath || _agent.pathStatus == NavMeshPathStatus.PathInvalid)
            _agent.SetDestination(_patrolTarget);

        bool arrived = !_agent.pathPending
                    && _agent.remainingDistance <= _agent.stoppingDistance + 0.2f
                    && _agent.velocity.sqrMagnitude < 0.05f;
        if (arrived) { _patrolWaitTimer = patrolWaitTime; _state = State.Idle; }
    }

    private void UpdateChase()
    {
        if (_player == null) { _state = State.Idle; return; }
        float dist = Vector3.Distance(transform.position, _player.position);

        if (dist <= attackRange)
        {
            _state = State.Attacking;
            _attackingExitTimer = 0f;
            if (_agent.isOnNavMesh) _agent.isStopped = true;
            _animator?.SetBool(HashIsWalking, false);
            _animator?.SetBool(HashIsRunning, false);
        }
        else
        {
            _agent.isStopped = false;
            if (_agent.isOnNavMesh) _agent.SetDestination(_player.position);
            _animator?.SetBool(HashIsWalking, false);
            _animator?.SetBool(HashIsRunning, true);
        }

        if (dist > detectionRange * 2f)
        {
            if (_inCombat) { _inCombat = false; GameMusicManager.Instance?.ExitCombat(); }
            _animator?.SetBool(HashIsRunning, false);
            _state = State.Patrolling; _player = null;
        }
    }

    private void UpdateAttack()
    {
        if (_player == null) { _state = State.Idle; return; }
        float dist = Vector3.Distance(transform.position, _player.position);

        Vector3 dir = _player.position - transform.position; dir.y = 0f;
        if (dir != Vector3.zero)
            transform.rotation = Quaternion.Slerp(transform.rotation, Quaternion.LookRotation(dir), 10f * Time.deltaTime);

        _attackingExitTimer += Time.deltaTime;
        if (_attackingExitTimer >= 0.4f && dist > attackRange * 1.5f)
        {
            _state = State.Chasing;
            if (_agent.isOnNavMesh) _agent.isStopped = false;
            return;
        }

        if (Time.time >= _lastAttackTime + attackCooldown && _player != null && _player.CompareTag("Player"))
        {
            _lastAttackTime = Time.time;
            _animator?.SetTrigger(_attackStep % 2 == 0 ? HashAttack : HashAttack2);
            _attackStep++;
            var ph = _player.GetComponentInParent<PlayerHealth>() ?? _player.GetComponentInChildren<PlayerHealth>();
            ph?.TakeDamage(attackDamage);
        }
    }

    public void TakeDamage(float amount)
    {
        if (_state == State.Dead) return;
        _health -= amount;
        if (_player == null)
        {
            _player = GameObject.FindGameObjectWithTag("Player")?.transform;
            if (!_inCombat) { _inCombat = true; GameMusicManager.Instance?.EnterCombat(); }
            _state = State.Chasing;
        }
        _animator?.SetTrigger(HashTakeDamage);
        if (_health <= 0f) Die();
    }

    private void Die()
    {
        _state = State.Dead;
        if (_inCombat) { _inCombat = false; GameMusicManager.Instance?.ExitCombat(); }
        if (_agent.isOnNavMesh) _agent.isStopped = true;
        _agent.enabled = false;
        // Voltar ao idle antes de morrer (pose neutra, nao pose de ataque)
        _animator?.SetBool(HashIsWalking, false);
        _animator?.SetBool(HashIsRunning, false);
        _animator?.Play("Idle");
        var col = GetComponent<CapsuleCollider>();
        if (col != null) col.isTrigger = true;
        PlayerInventory.Instance?.AddCoins(coinReward);
        StartCoroutine(DeathRoutine());
    }

    private IEnumerator DeathRoutine()
    {
        // Deixar transicionar para idle (0.4s)
        yield return new WaitForSeconds(0.4f);
        // Congelar na pose idle
        if (_animator != null) _animator.enabled = false;
        // Encolher igual ao skeleton
        float elapsed = 0f;
        float shrinkDuration = 1.2f;
        Vector3 originalScale = transform.localScale;
        while (elapsed < shrinkDuration)
        {
            elapsed += Time.deltaTime;
            transform.localScale = Vector3.Lerp(originalScale, Vector3.zero,
                Mathf.Clamp01(elapsed / shrinkDuration));
            yield return null;
        }
        Destroy(gameObject);
    }

    private void PickPatrolTarget()
    {
        for (int i = 1; i <= 8; i++)
        {
            Vector2 c = Random.insideUnitCircle.normalized * patrolRadius;
            Vector3 candidate = _spawnPosition + new Vector3(c.x, 0, c.y);
            if (NavMesh.SamplePosition(candidate, out NavMeshHit hit, patrolRadius * i * 0.5f, NavMesh.AllAreas))
                if (Vector3.Distance(transform.position, hit.position) > 1.5f)
                { _patrolTarget = hit.position; return; }
        }
        _patrolTarget = transform.position + transform.forward * patrolRadius * 0.5f;
    }

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.cyan; Gizmos.DrawWireSphere(transform.position, detectionRange);
        Gizmos.color = Color.red;  Gizmos.DrawWireSphere(transform.position, attackRange);
    }
}















