using UnityEngine;
using UnityEngine.AI;
using UnityEngine.SceneManagement;
using System.Collections;

[RequireComponent(typeof(NavMeshAgent))]
public class CapraDemonController : MonoBehaviour, IDamageable, IStaggerable
{
    [Header("Stats")]
    public float maxHealth         = 200f;
    public float lightAttackDamage = 10f;
    public float heavyAttackDamage = 20f;
    public float attackCooldown    = 1.6f;
    public float moveSpeed      = 2.5f;

    [Header("Detection")]
    public float detectionRange = 5f;
    public float attackRange    = 1.2f;

    [Header("Patrol")]
    public float patrolRadius   = 6f;
    public float patrolWaitTime = 3f;

    [Header("Rewards")]
    public int coinReward = 10;

    [Header("Boss HUD")]
    [SerializeField] private string displayName = "O Bode Viciado";

    [Header("Memory Drop")]
    [SerializeField] private GameObject memoryPrefab;
    public int videoIndex = 1;

    private static readonly int HashIsWalking  = Animator.StringToHash("IsWalking");
    private static readonly int HashIsRunning  = Animator.StringToHash("IsRunning");
    private static readonly int HashAttack     = Animator.StringToHash("Attack");
    private static readonly int HashAttack2    = Animator.StringToHash("Attack2");
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
    private bool _inCombat;
    private PlayerHealth _playerHealth;

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
        // Se o player morreu, voltar a patrulhar
        if (_player != null)
        {
            if (_playerHealth == null) _playerHealth = _player.GetComponentInParent<PlayerHealth>() ?? _player.GetComponentInChildren<PlayerHealth>();
            if (_playerHealth != null && _playerHealth.IsDead)
            {
                _player = null; _playerHealth = null;
                if (_inCombat) { _inCombat = false; GameMusicManager.Instance?.ExitCombat(); BossHUDController.Instance?.Hide(); }
                _animator?.SetBool(HashIsWalking, false); _animator?.SetBool(HashIsRunning, false);
                _animator?.Play("Idle");
                if (_agent.isOnNavMesh) _agent.isStopped = true;
                _state = State.Idle;
                return;
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
            if (!_inCombat) { _inCombat = true; GameMusicManager.Instance?.EnterCombat(); BossHUDController.Instance?.SetName(displayName); BossHUDController.Instance?.Show(); BossHUDController.Instance?.Refresh(_health, maxHealth); }
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
            if (_agent.isOnNavMesh) { _agent.isStopped = false; _agent.speed = moveSpeed; _agent.SetDestination(_player.position); }
            _animator?.SetBool(HashIsWalking, false); _animator?.SetBool(HashIsRunning, true);
        }
        if (dist > detectionRange * 2f) { if (_inCombat) { _inCombat = false; GameMusicManager.Instance?.ExitCombat(); } BossHUDController.Instance?.Hide(); _animator?.SetBool(HashIsRunning, false); _state = State.Patrolling; _player = null; }
    }

    private void UpdateAttack()
    {
        if (_player == null) { _state = State.Idle; return; }
        float dist = Vector3.Distance(transform.position, _player.position);
        Vector3 dir = _player.position - transform.position; dir.y = 0f;
        if (dir != Vector3.zero) transform.rotation = Quaternion.Slerp(transform.rotation, Quaternion.LookRotation(dir), 10f * Time.deltaTime);
        if (_animator != null)
        {
            var info = _animator.GetCurrentAnimatorStateInfo(0);
            if ((info.IsName("Attack1") || info.IsName("Attack2")) && info.normalizedTime < 0.9f) return;
        }
        _attackingExitTimer += Time.deltaTime;
        if (_attackingExitTimer >= 0.4f && dist > attackRange * 1.5f) { _state = State.Chasing; if (_agent.isOnNavMesh) _agent.isStopped = false; return; }
        if (Time.time >= _lastAttackTime + attackCooldown && _player != null && _player.CompareTag("Player"))
        {
            _lastAttackTime = Time.time;
            bool isHeavy = _attackStep % 2 == 1;
            _animator?.SetTrigger(_attackStep % 2 == 0 ? HashAttack : HashAttack2); _attackStep++;
            var ph = _player.GetComponentInParent<PlayerHealth>() ?? _player.GetComponentInChildren<PlayerHealth>();
            if (ph != null && !ph.IsDead) ph.TakeDamage(isHeavy ? heavyAttackDamage : lightAttackDamage);
        }
    }

    public void TakeDamage(float amount)
    {
        if (_state == State.Dead) return;
        _health -= amount;
        if (_player == null) { _player = GameObject.FindGameObjectWithTag("Player")?.transform; if (!_inCombat) { _inCombat = true; GameMusicManager.Instance?.EnterCombat(); BossHUDController.Instance?.SetName(displayName); BossHUDController.Instance?.Show(); } _state = State.Chasing; }
        BossHUDController.Instance?.Refresh(_health, maxHealth);
        if (_health <= 0f) Die();
    }

    public void Stagger()
    {
        if (_state == State.Dead) return;
        _animator?.SetTrigger(HashTakeDamage);
    }

    private void Die()
    {
        _state = State.Dead;
        if (_inCombat) { _inCombat = false; GameMusicManager.Instance?.ExitCombat(); }
        BossHUDController.Instance?.Hide();
        if (_agent.isOnNavMesh) _agent.isStopped = true;
        _agent.enabled = false;
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
        if (memoryPrefab != null)
        {
            var mem = Instantiate(memoryPrefab, transform.position + Vector3.up * 1f, Quaternion.identity);
            var pickup = mem.GetComponent<MemoryPickup>();
            if (pickup != null)
            {
                string sceneName = SceneManager.GetActiveScene().name;
                pickup.videoIndex = videoIndex;
                pickup.completedLevelSceneName = sceneName;

                int completedOtherLevels = 0;
                if (sceneName != "level_1" && LevelCompletionState.IsCompleted("level_1")) completedOtherLevels++;
                if (sceneName != "level_2" && LevelCompletionState.IsCompleted("level_2")) completedOtherLevels++;
                if (sceneName != "level_3" && LevelCompletionState.IsCompleted("level_3")) completedOtherLevels++;

                int memoryNumber = Mathf.Clamp(completedOtherLevels + 1, 1, 3);
                pickup.videoFileName = $"{memoryNumber}_memoria_v2";
                pickup.targetSceneName = "masmorra_spawn";
            }
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

















